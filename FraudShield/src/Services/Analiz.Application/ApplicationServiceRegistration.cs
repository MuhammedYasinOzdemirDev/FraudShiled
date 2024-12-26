using System.Reflection;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Services;
using Analiz.ML.Evaluator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;

namespace Analiz.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();
        services.AddScoped<IModelPredictionService>(sp => 
            (IModelPredictionService)sp.GetRequiredService<IModelService>());
        services.AddScoped<IModelEvaluator, ModelEvaluator>();
        services.AddSingleton<MLContext>();
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
