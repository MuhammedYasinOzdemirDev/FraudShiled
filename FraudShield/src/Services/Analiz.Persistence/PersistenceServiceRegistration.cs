using Analiz.Application.Interfaces.Repositories;
using Analiz.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Analiz.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection"))
        {
            MinPoolSize = 5,
            MaxPoolSize = 50,
            ConnectionIdleLifetime = 300, // 5 dakika
            Pooling = true
        };

        var connectionString = builder.ToString();

        // DbContext yapılandırması
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: new[] { "53300" }); // too many clients hatası için
            });

            // DB Context ayarları
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }, poolSize: 128); // DbContext pool size artırıldı

        // Factory kaydı
        services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
            });
        });

        services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IFeatureConfigurationRepository, FeatureConfigurationRepository>();

        return services;
    }
}