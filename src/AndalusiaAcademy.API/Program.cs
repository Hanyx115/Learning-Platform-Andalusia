using AndalusiaAcademy.API.HealthChecks;
using AndalusiaAcademy.API.Middleware;
using AndalusiaAcademy.API.OpenApi;
using AndalusiaAcademy.API.Configuration;
using AndalusiaAcademy.Application;
using AndalusiaAcademy.Infrastructure;
using AndalusiaAcademy.Infrastructure.Catalog.Seeding;
using AndalusiaAcademy.Infrastructure.Content.Seeding;
using AndalusiaAcademy.Infrastructure.Data;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using AndalusiaAcademy.Infrastructure.Identity.Seeding;
using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(builder.Configuration, builder.Environment.EnvironmentName);

if (builder.Environment.IsProduction())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.TimestampFormat = "O";
        options.UseUtcTimestamp = true;
        options.IncludeScopes = true;
    });
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddOptions<AuthRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(AuthRateLimitOptions.SectionName))
    .ValidateOnStart();
builder.Services
    .AddOptions<RefreshTokenCookieOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenCookieOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "The refresh-token cookie name is required.")
    .ValidateOnStart();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests.",
                Detail = "Please wait before trying this authentication action again.",
                Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier },
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };

    options.AddPolicy(AuthRateLimitPolicies.Login, httpContext =>
    {
        var rateLimits = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        return CreateIpRateLimiter(httpContext, rateLimits.LoginPermitLimit, rateLimits.LoginWindowSeconds);
    });
    options.AddPolicy(AuthRateLimitPolicies.Registration, httpContext =>
    {
        var rateLimits = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        return CreateIpRateLimiter(httpContext, rateLimits.RegistrationPermitLimit, rateLimits.RegistrationWindowSeconds);
    });
    options.AddPolicy(AuthRateLimitPolicies.Recovery, httpContext =>
    {
        var rateLimits = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        return CreateIpRateLimiter(httpContext, rateLimits.RecoveryPermitLimit, rateLimits.RecoveryWindowSeconds);
    });
    options.AddPolicy(AuthRateLimitPolicies.Session, httpContext =>
    {
        var rateLimits = httpContext.RequestServices.GetRequiredService<IOptions<AuthRateLimitOptions>>().Value;
        return CreateIpRateLimiter(httpContext, rateLimits.SessionPermitLimit, rateLimits.SessionWindowSeconds);
    });
    options.AddPolicy("course-assistant", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
builder.Services.AddHealthChecks()
    .AddCheck<SelfHealthCheck>("self", tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<NotificationOutboxHealthCheck>("notification-outbox", tags: ["ready"]);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
}).AddOpenApi(options =>
{
    options.Document.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.Document.AddOperationTransformer<BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    var migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
    var seedCatalog = app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("CatalogSeed:Enabled");
    var seedIdentity = app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("IdentitySeed:Enabled");

    if (migrateOnStartup || seedCatalog || seedIdentity)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!dbContext.Database.IsRelational())
        {
            app.Logger.LogInformation(
                "Development database initialization skipped for a non-relational database provider.");
        }
        else
        {
            if (migrateOnStartup)
            {
                await dbContext.Database.MigrateAsync();
                app.Logger.LogInformation("Development database migrations are current.");
            }

            if (seedCatalog && !await dbContext.Database.CanConnectAsync())
            {
                app.Logger.LogWarning(
                    "Catalog demo data seeding skipped because the database is unavailable.");
            }
            else if (seedCatalog)
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    app.Logger.LogWarning(
                        "Catalog demo data seeding skipped because database migrations are pending.");
                }
                else
                {
                    var addedRows = await CatalogDataSeeder.SeedAsync(dbContext);
                    app.Logger.LogInformation(
                        "Catalog demo data seeding completed. Added {AddedRows} missing rows.",
                        addedRows);
                }
            }

            if (seedIdentity && !await dbContext.Database.CanConnectAsync())
            {
                app.Logger.LogWarning(
                    "Identity demo account seeding skipped because the database is unavailable.");
            }
            else if (seedIdentity)
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    app.Logger.LogWarning(
                        "Identity demo account seeding skipped because database migrations are pending.");
                }
                else
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var addedUsers = await IdentityDemoSeeder.SeedAsync(roleManager, userManager);
                    var addedInstructorProfiles = await CmsDemoSeeder.SeedInstructorProfileAsync(dbContext, userManager);
                    app.Logger.LogInformation(
                        "Identity and team demo seeding completed. Added {AddedUsers} missing accounts and {AddedInstructorProfiles} instructor profiles.",
                        addedUsers,
                        addedInstructorProfiles);
                }
            }
        }
    }

    app.MapOpenApi().WithDocumentPerVersion();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "Andalusia Academy API v1");
        options.ExposeSwaggerDocumentUrlsRoute = true;
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync,
});

var publicUrls = app.Configuration["PUBLIC_URLS"]?.Split(
    ';',
    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var publicUrl in publicUrls)
    {
        app.Logger.LogInformation(
            "OpenAPI URL: {OpenApiUrl}",
            $"{publicUrl.TrimEnd('/')}/openapi/v1.json");
        app.Logger.LogInformation(
            "Swagger UI URL: {SwaggerUrl}",
            $"{publicUrl.TrimEnd('/')}/swagger");
    }
});

app.Run();

static RateLimitPartition<string> CreateIpRateLimiter(
    HttpContext httpContext,
    int permitLimit,
    int windowSeconds) =>
    RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Max(1, permitLimit),
            Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds)),
            QueueLimit = 0,
            AutoReplenishment = true,
        });

public partial class Program;
