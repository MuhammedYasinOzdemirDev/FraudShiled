using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Application.Interfaces.Training;
using Analiz.Application.Services.Training;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using Analiz.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;
/*
[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
    // Controller artık constructor injection kullanmıyor
    public ModelController()
    {
    }

    [HttpGet("test-data")]
    public IActionResult TestDataLoading()
    {
        try
        {
            return Ok("Test data loaded successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while loading test data");
        }
    }
}*/

[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
   private readonly IModelService _modelService;
    private readonly ILogger<ModelController> _logger;
    private readonly IFraudModelTrainingService _trainingService;
    private readonly ITestDataService _testDataService;

    public ModelController(
        IModelService modelService,
        ILogger<ModelController> logger,
        IFraudModelTrainingService trainingService,
        ITestDataService testDataService)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
       _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
        _testDataService = testDataService ?? throw new ArgumentNullException(nameof(testDataService));
    }


    [HttpGet("test-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestDataLoading()
    {
        try
        {
            _logger.LogInformation("Loading test data");
            var data = await _testDataService.LoadCreditCardDataAsync();
             return Ok(new
             {
                 TotalRecords = data.Count,
                 SampleRecords = data.Take(5).Select(x => new
                 {
                     x.Time,
                     x.Amount,
                     x.Label,
                     V1_V5 = new[] { x.V1, x.V2, x.V3, x.V4, x.V5 }
                 })
             });
        }
        catch (Exception ex)
        {
           _logger.LogError(ex, "Error loading test data");
            return StatusCode(500, "An error occurred while loading test data");
        }
    }
    
    [HttpPost("credit-card-train")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrainModels()
    {
        try
        {
            _logger.LogInformation("Starting credit card model training");
            var results = await _trainingService.TrainModelsAsync();

            return Ok(new
            {
                LightGBMMetrics = results.LightGBM.Metrics,
                PCAMetrics = results.PCA.Metrics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during credit card model training");
            return StatusCode(500, "An error occurred during model training");
        }
    }
    /// <summary>
    /// Sadece ensemble modeli eğitir
    /// </summary>
    [HttpPost("train-ensemble")]
    [ProducesResponseType(typeof(Response.EnsembleTrainingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrainEnsembleModel()
    {
        try
        {
            _logger.LogInformation("Starting ensemble model training");
            var result = await _trainingService.TrainEnsembleModelAsync();

            var response = new Response.EnsembleTrainingResponse
            {
                ModelId = result.ModelId,
                TrainingTime = result.TrainingTime,
                Metrics = result.Metrics,
                ModelVersion = result.ModelId.ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ensemble model training");
            return StatusCode(500, "An error occurred during ensemble model training");
        }
    }

    /// <summary>
    /// Tüm modelleri (LightGBM, PCA ve Ensemble) paralel olarak eğitir
    /// </summary>
    [HttpPost("train-all")]
    [ProducesResponseType(typeof(Response.ComprehensiveTrainingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrainAllModels()
    {
        try
        {
            _logger.LogInformation("Starting comprehensive model training");
            var results = await _trainingService.TrainAllModelsAsync();

            var response = new Response.ComprehensiveTrainingResponse
            {
                LightGBM = new Response.ModelTrainingResponse
                {
                    ModelId = results.LightGBMResult.ModelId,
                    TrainingTime = results.LightGBMResult.TrainingTime,
                    Metrics = results.LightGBMResult.Metrics,
                    ModelVersion = results.LightGBMResult.ModelId.ToString()
                },
                PCA = new Response.ModelTrainingResponse
                {
                    ModelId = results.PCAResult.ModelId,
                    TrainingTime = results.PCAResult.TrainingTime,
                    Metrics = results.PCAResult.Metrics,
                    ModelVersion = results.PCAResult.ModelId.ToString()
                },
                Ensemble = new Response.ModelTrainingResponse
                {
                    ModelId = results.EnsembleResult.ModelId,
                    TrainingTime = results.EnsembleResult.TrainingTime,
                    Metrics = results.EnsembleResult.Metrics,
                    ModelVersion = results.EnsembleResult.ModelId.ToString()
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during comprehensive model training");
            return StatusCode(500, "An error occurred during model training");
        }
    }



    [HttpPost("train")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TrainingResult>> TrainModel(TrainingRequest request)
    {
        try
        {
            _logger.LogInformation("Starting model training with request {@Request}", request);
            var result = await _modelService.TrainModelAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model");
            return StatusCode(500, "An error occurred during model training");
        }
    }

    [HttpPost("evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EvaluationResult>> EvaluateModel(EvaluationRequest request)
    {
        try
        {
            var result = await _modelService.EvaluateModelAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model");
            return StatusCode(500, "An error occurred during model evaluation");
        }
    }

    [HttpGet("{modelName}/metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ModelMetrics>> GetModelMetrics(string modelName)
    {
        try
        {
            var metrics = await _modelService.GetModelMetricsAsync(modelName);
            if (metrics == null)
                return NotFound($"No metrics found for model: {modelName}");

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics for model {ModelName}", modelName);
            return StatusCode(500, "An error occurred while retrieving model metrics");
        }
    }

    [HttpPut("{modelName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateModel(string modelName, ModelUpdateRequest request)
    {
        try
        {
            var success = await _modelService.UpdateModelAsync(modelName, request);
            if (!success)
                return NotFound($"Model not found: {modelName}");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model {ModelName}", modelName);
            return StatusCode(500, "An error occurred while updating the model");
        }
    }

    [HttpGet("{modelName}/versions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ModelVersion>>> GetModelVersions(string modelName)
    {
        try
        {
            var versions = await _modelService.GetModelVersionsAsync(modelName);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving versions for model {ModelName}", modelName);
            return StatusCode(500, "An error occurred while retrieving model versions");
        }
    }
    [HttpGet("test-injection")]
    public IActionResult TestInjection()
    {
        if (_modelService == null) return BadRequest("IModelService not injected");
        if (_testDataService == null) return BadRequest("ITestDataService not injected");
        if (_trainingService == null) return BadRequest("IFraudModelTrainingService not injected");
        return Ok("All services are correctly injected");
    }

    [HttpPost("{modelName}/versions/{version}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ActivateModelVersion(string modelName, string version)
    {
        try
        {
            var success = await _modelService.ActivateModelVersionAsync(modelName, version);
            if (!success)
                return NotFound($"Model version not found: {modelName}/{version}");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating version {Version} for model {ModelName}", version, modelName);
            return StatusCode(500, "An error occurred while activating the model version");
        }
    }
}
