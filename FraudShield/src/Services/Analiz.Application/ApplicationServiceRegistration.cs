using System.Reflection;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Training;
using Analiz.Application.Services;
using Analiz.Application.Services.Training;
using Analiz.Domain.Entities;
using Analiz.ML.Evaluator;
using Analiz.ML.Models.LightGBM;
using Analiz.ML.Models.PCA;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace Analiz.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IFeatureExtractionService, FeatureEngineeringService>();
        services.AddScoped<IFraudRuleEngine, FraudRuleEngine>();
        services.Configure<PCAConfiguration>(configuration.GetSection("PCAConfiguration"));
        services.Configure<LightGBMConfiguration>(configuration.GetSection("LightGBMConfiguration"));
        


        services.AddScoped<IModelEvaluator, ModelEvaluator>();
        services.AddSingleton<MLContext>();
        
        services.AddScoped<IFraudModelTrainingService, FraudModelTrainingService>();

        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
