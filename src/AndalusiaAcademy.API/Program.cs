using AndalusiaAcademy.API.HealthChecks;
using AndalusiaAcademy.API.Middleware;
using AndalusiaAcademy.Application;
using AndalusiaAcademy.Infrastructure;
using AndalusiaAcademy.Infrastructure.Catalog.Seeding;
using AndalusiaAcademy.Infrastructure.Data;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
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
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddHealthChecks()
    .AddCheck<SelfHealthCheck>("self");
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
}).AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    if (app.Configuration.GetValue<bool>("CatalogSeed:Enabled"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!dbContext.Database.IsRelational())
        {
            app.Logger.LogInformation("Catalog demo data seeding skipped for a non-relational database provider.");
        }
        else if (!await dbContext.Database.CanConnectAsync())
        {
            app.Logger.LogWarning(
                "Catalog demo data seeding skipped because the database is unavailable. Apply migrations and restart the API.");
        }
        else
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations.Any())
            {
                app.Logger.LogWarning(
                    "Catalog demo data seeding skipped because database migrations are pending. Apply migrations and restart the API.");
            }
            else
            {
                var addedRows = await CatalogDataSeeder.SeedAsync(dbContext);
                app.Logger.LogInformation(
                    "Catalog demo data seeding completed. Added {AddedRows} missing rows.",
                    addedRows);
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

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

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

public partial class Program;
