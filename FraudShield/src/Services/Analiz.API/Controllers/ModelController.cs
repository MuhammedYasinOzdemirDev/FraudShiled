using Analiz.Application.Interfaces;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
    private readonly IModelService _modelService;
    private readonly ILogger<ModelController> _logger;

    public ModelController(IModelService modelService, ILogger<ModelController> logger)
    {
        _modelService = modelService;
        _logger = logger;
    }

    [HttpPost("train")]
    public async Task<ActionResult<TrainingResult>> TrainModel(TrainingRequest request)
    {
        try
        {
            var result = await _modelService.TrainModelAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<EvaluationResult>> EvaluateModel(EvaluationRequest request)
    {
        var result = await _modelService.EvaluateModelAsync(request);
        return Ok(result);
    }

    [HttpGet("{modelName}/metrics")]
    public async Task<ActionResult<ModelMetrics>> GetModelMetrics(string modelName)
    {
        var metrics = await _modelService.GetModelMetricsAsync(modelName);
        if (metrics == null)
            return NotFound();
        return Ok(metrics);
    }

    [HttpPut("{modelName}")]
    public async Task<ActionResult> UpdateModel(string modelName, ModelUpdateRequest request)
    {
        var success = await _modelService.UpdateModelAsync(modelName, request);
        if (!success)
            return NotFound();
        return Ok();
    }

    [HttpGet("{modelName}/versions")]
    public async Task<ActionResult<List<ModelVersion>>> GetModelVersions(string modelName)
    {
        var versions = await _modelService.GetModelVersionsAsync(modelName);
        return Ok(versions);
    }

    [HttpPost("{modelName}/versions/{version}/activate")]
    public async Task<ActionResult> ActivateModelVersion(string modelName, string version)
    {
        var success = await _modelService.ActivateModelVersionAsync(modelName, version);
        if (!success)
            return NotFound();
        return Ok();
    }
}
