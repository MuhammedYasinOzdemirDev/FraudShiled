namespace Analiz.Worker.BackgroundServices;

public class AdaptiveLearningService : BackgroundService
{
    private readonly ILogger<AdaptiveLearningService> _logger;
    private readonly IModelTrainingService _modelTrainingService;
    private readonly IModelRepository _modelRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly AdaptiveLearningOptions _options;
    private readonly ConcurrentQueue<TransactionData> _transactionBuffer;

    public AdaptiveLearningService(
        ILogger<AdaptiveLearningService> logger,
        IModelTrainingService modelTrainingService,
        IModelRepository modelRepository,
        ITransactionRepository transactionRepository,
        IFeatureExtractor featureExtractor,
        IOptions<AdaptiveLearningOptions> options)
    {
        _logger = logger;
        _modelTrainingService = modelTrainingService;
        _modelRepository = modelRepository;
        _transactionRepository = transactionRepository;
        _featureExtractor = featureExtractor;
        _options = options.Value;
        _transactionBuffer = new ConcurrentQueue<TransactionData>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTransactionBufferAsync(stoppingToken);
                await CheckModelPerformanceAsync(stoppingToken);
                await Task.Delay(_options.UpdateInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in adaptive learning service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task ProcessTransactionBufferAsync(CancellationToken stoppingToken)
    {
        if (_transactionBuffer.Count >= _options.BatchUpdateThreshold)
        {
            var batchData = new List<TransactionData>();
            while (_transactionBuffer.TryDequeue(out var transaction))
            {
                batchData.Add(transaction);
            }

            await UpdateModelWithBatchAsync(batchData, stoppingToken);
        }
    }

    private async Task UpdateModelWithBatchAsync(
        List<TransactionData> transactions, 
        CancellationToken stoppingToken)
    {
        var features = await _featureExtractor.ExtractBatchFeaturesAsync(transactions);
        var trainingData = new TrainingData
        {
            Features = features,
            Labels = transactions.Select(t => t.IsFraudulent).ToArray()
        };

        await _modelTrainingService.UpdateModelAsync("FraudDetector", trainingData, stoppingToken);
    }
}
