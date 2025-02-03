using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeatureExtractionController : ControllerBase
{
    private readonly IFeatureExtractionService _featureService;
    private readonly ILogger<FeatureExtractionController> _logger;

    public FeatureExtractionController(
        IFeatureExtractionService featureService,
        ILogger<FeatureExtractionController> logger)
    {
        _featureService = featureService;
        _logger = logger;
    }

    [HttpPost("extract")]
    public async Task<ActionResult<FeatureSet>> ExtractFeatures(TransactionData data)
    {
        var features = await _featureService.ExtractFeaturesAsync(data);
        return Ok(features);
    }

    [HttpPost("extract/batch")]
    public async Task<ActionResult<List<FeatureSet>>> ExtractBatchFeatures(List<TransactionData> data)
    {
        var features = await _featureService.ExtractBatchFeaturesAsync(data);
        return Ok(features);
    }
/*
    [HttpGet("models/{modelName}/importance")]
    public async Task<ActionResult<FeatureImportance>> GetFeatureImportance(string modelName)
    {
        var importance = await _featureService.GetFeatureImportanceAsync(modelName);
        if (importance == null)
            return NotFound();
        return Ok(importance);
    }*/

    [HttpPut("configuration")]
    public async Task<ActionResult> UpdateConfiguration(FeatureConfig config)
    {
        var success = await _featureService.UpdateFeatureConfigurationAsync(config);
        if (!success)
            return BadRequest("Failed to update feature configuration");
        return Ok();
    }
}