namespace Analiz.Worker.BackgroundServices;
public class ModelHealthCheckService : BackgroundService
{
    private readonly ILogger<ModelHealthCheckService> _logger;
    private readonly IModelEvaluator _modelEvaluator;
    private readonly IModelRepository _modelRepository;
    private readonly HealthCheckOptions _options;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckModelsHealthAsync(stoppingToken);
                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in model health check service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckModelsHealthAsync(CancellationToken stoppingToken)
    {
        var activeModels = await _modelRepository.GetActiveModelsAsync();
        
        foreach (var model in activeModels)
        {
            var healthReport = await _modelEvaluator.EvaluateModelHealthAsync(model.Name);
            
            if (healthReport.RequiresRetraining)
            {
                _logger.LogWarning("Model {ModelName} requires retraining. Triggering retraining job...", 
                    model.Name);
                await TriggerRetrainingJobAsync(model.Name, stoppingToken);
            }
        }
    }
}