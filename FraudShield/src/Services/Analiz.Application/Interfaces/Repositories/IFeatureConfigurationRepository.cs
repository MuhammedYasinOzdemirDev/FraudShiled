using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;
public interface IFeatureConfigurationRepository
{
    Task<FeatureConfiguration> GetActiveConfigurationAsync();
    Task UpdateAsync(FeatureConfiguration configuration);
}