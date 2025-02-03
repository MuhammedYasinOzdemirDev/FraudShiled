using Analiz.Application.Interfaces;
using Analiz.Application.Services.Training;
using Analiz.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class FraudDetectionController : ControllerBase
{
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<FraudDetectionController> _logger;

   
    public FraudDetectionController(
        IFraudDetectionService fraudDetectionService,
        ILogger<FraudDetectionController> logger)
    {
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }


    [HttpPost("analyze")]
    public async Task<ActionResult<AnalysisResult>> AnalyzeTransaction(TransactionRequest request)
    {
        try
        {
            var result = await _fraudDetectionService.AnalyzeTransactionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing transaction");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("analyze/batch")]
    public async Task<ActionResult<List<AnalysisResult>>> AnalyzeBatch(List<TransactionRequest> requests)
    {
        try
        {
            var results = await _fraudDetectionService.AnalyzeBatchAsync(requests);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing batch transactions");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("analysis/{analysisId}")]
    public async Task<ActionResult<AnalysisResult>> GetAnalysisResult(Guid analysisId)
    {
        var result = await _fraudDetectionService.GetAnalysisResultAsync(analysisId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost("evaluate-risk")]
    public async Task<ActionResult<RiskEvaluation>> EvaluateRisk(TransactionData data)
    {
        var evaluation = await _fraudDetectionService.EvaluateRiskAsync(data);
        return Ok(evaluation);
    }

    [HttpPut("rules")]
    public async Task<ActionResult> UpdateFraudRules(List<FraudRule> rules)
    {
        var success = await _fraudDetectionService.UpdateFraudRulesAsync(rules);
        if (!success)
            return BadRequest("Failed to update fraud rules");
        return Ok();
    }

    [HttpGet("alerts/active")]
    public async Task<ActionResult<List<FraudAlert>>> GetActiveAlerts()
    {
        var alerts = await _fraudDetectionService.GetActiveAlertsAsync();
        return Ok(alerts);
    }
}
