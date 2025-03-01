using System.Text.Json;
using System.Text.Json.Serialization;
using Analiz.Application;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Application.Interfaces.Training;
using Analiz.Infrastructure;
using Analiz.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

// Konsol loglama sağlayıcısını ekle ve formatı özelleştir
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false; // Scopes'ları gösterme
    options.SingleLine = true; // Tek satırlık loglar
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; // Zaman damgasını özelleştir
});
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fraud Analysis API",
        Version = "v1",
        Description = "API for fraud detection and analysis"
    });
    c.UseInlineDefinitionsForEnums();
});

// Add other services
builder.Services.AddMemoryCache();

// Register application services
builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fraud Analysis API V1");
    c.RoutePrefix = "swagger";
    c.EnableTryItOutByDefault();
    c.DefaultModelsExpandDepth(-1); // Hide schemas section
});

// Basic middleware pipeline
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

// Global error handling - after routing, before endpoints
/*
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        var error = new
        {
            Status = context.Response.StatusCode,
            Message = exception?.Message,
            Detail = app.Environment.IsDevelopment() ? exception?.ToString() : null
        };

        await context.Response.WriteAsJsonAsync(error);
    });
});
*/
// Map endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    // Health check endpoint
    endpoints.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
});


await app.RunAsync();