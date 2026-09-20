using AndalusiaAcademy.API.Middleware;
using AndalusiaAcademy.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

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
    }
});

app.Run();

public partial class Program;
