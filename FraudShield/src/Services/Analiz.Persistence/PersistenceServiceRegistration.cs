using Analiz.Application.Interfaces.Repositories;
using Analiz.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Analiz.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IFeatureConfigurationRepository, FeatureConfigurationRepository>();

        return services;
    }
}