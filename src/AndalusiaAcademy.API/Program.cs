using AndalusiaAcademy.API.HealthChecks;
using AndalusiaAcademy.API.Middleware;
using AndalusiaAcademy.Application;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplication();
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
