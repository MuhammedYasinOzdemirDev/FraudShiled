using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class FeatureConfigurationRepository : IFeatureConfigurationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FeatureConfigurationRepository> _logger;

    public FeatureConfigurationRepository(
        ApplicationDbContext context,
        ILogger<FeatureConfigurationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeatureConfiguration> GetActiveConfigurationAsync()
    {
        try
        {
            return await _context.FeatureConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive && !x.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active feature configuration");
            throw new RepositoryException("Failed to retrieve active feature configuration", ex);
        }
    }

    public async Task UpdateAsync(FeatureConfiguration configuration)
    {
        try
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Deactivate current active configuration if exists
            var currentActive = await GetActiveConfigurationAsync();
            if (currentActive != null && currentActive.Id != configuration.Id)
            {
                currentActive.Deactivate();
                _context.FeatureConfigurations.Update(currentActive);
            }

            _context.FeatureConfigurations.Update(configuration);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated feature configuration {ConfigId} with version {Version}", 
                configuration.Id, 
                configuration.Version);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict while updating feature configuration {ConfigId}", 
                configuration.Id);
            throw new ConcurrencyException("Configuration was modified by another process", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature configuration {ConfigId}", 
                configuration.Id);
            throw new RepositoryException("Failed to update feature configuration", ex);
        }
    }

    public async Task<FeatureConfiguration> CreateAsync(FeatureConfiguration configurationData)
    {
        try
        {
            if (configurationData == null)
                throw new ArgumentNullException(nameof(configurationData));

            // Use the static factory method to create a new instance
            var configuration = FeatureConfiguration.Create(
                configurationData.EnabledFeatures,
                configurationData.FeatureSettings,
                configurationData.NormalizationParameters);

            await _context.FeatureConfigurations.AddAsync(configuration);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created new feature configuration {ConfigId} with version {Version}",
                configuration.Id,
                configuration.Version);

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature configuration");
            throw new RepositoryException("Failed to create feature configuration", ex);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var configuration = await _context.FeatureConfigurations.FindAsync(id);
            if (configuration == null)
                return;

            configuration.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted feature configuration {ConfigId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature configuration {ConfigId}", id);
            throw new RepositoryException($"Failed to delete feature configuration with ID {id}", ex);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            return await _context.FeatureConfigurations
                .AsNoTracking()
                .AnyAsync(x => x.Id == id && !x.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of feature configuration {ConfigId}", id);
            throw new RepositoryException($"Failed to check existence of feature configuration with ID {id}", ex);
        }
    }
}

public class RepositoryException : Exception
{
    public RepositoryException(string message, Exception innerException = null) 
        : base(message, innerException)
    {
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, Exception innerException = null) 
        : base(message, innerException)
    {
    }
}