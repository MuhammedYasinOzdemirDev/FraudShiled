using System.Reflection;
using FraudShield.TransactionAnalysis.Domain.Models;
using FraudShield.TransactionAnalysis.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraudShield.TransactionAnalysis.Infrastructure;


public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName)));
        
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(AnalyticsDbContext).Assembly);
        });
        /* Redis Cache Registration
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "FraudShield_";
        });

        // Repository Registrations
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        services.AddScoped<IRiskProfileRepository, RiskProfileRepository>();
        services.AddScoped<IMLModelRepository, MLModelRepository>();

        // ML Services
        services.AddSingleton<IMLModelLoader, MLModelLoader>();
        services.AddScoped<IModelTrainingService, ModelTrainingService>();
        services.AddScoped<IModelPredictionService, ModelPredictionService>();
        services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();

        // Kafka Configuration
        services.AddSingleton<IKafkaProducerFactory>(sp =>
        {
            var kafkaConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                Acks = Acks.All
            };
            return new KafkaProducerFactory(kafkaConfig);
        });

        services.AddSingleton<IKafkaConsumerFactory>(sp =>
        {
            var kafkaConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = "analytics-service",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            return new KafkaConsumerFactory(kafkaConfig);
        });

        // Background Services
        services.AddHostedService<KafkaConsumerHostedService>();
        services.AddHostedService<ModelRetrainingHostedService>();
*/
        
        return services;
    }
}