using Analiz.Application.Interfaces;
using Analiz.Domain;
using Analiz.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Analiz.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class RiskScoringController : ControllerBase
{
    private readonly IRiskScoringService _riskScoringService;
    private readonly ILogger<RiskScoringController> _logger;

    public RiskScoringController(
        IRiskScoringService riskScoringService,
        ILogger<RiskScoringController> logger)
    {
        _riskScoringService = riskScoringService;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<RiskScore>> CalculateRiskScore(TransactionData data)
    {
        var score = await _riskScoringService.CalculateRiskScoreAsync(data);
        return Ok(score);
    }

    [HttpGet("users/{userId}/profile")]
    public async Task<ActionResult<RiskProfile>> GetUserRiskProfile(string userId)
    {
        var profile = await _riskScoringService.GetUserRiskProfileAsync(userId);
        if (profile == null)
            return NotFound();
        return Ok(profile);
    }

    [HttpPut("thresholds")]
    public async Task<ActionResult> UpdateRiskThresholds(RiskThresholds thresholds)
    {
        var success = await _riskScoringService.UpdateRiskThresholdsAsync(thresholds);
        if (!success)
            return BadRequest("Failed to update risk thresholds");
        return Ok();
    }

    [HttpGet("transactions/{transactionId}/factors")]
    public async Task<ActionResult<List<RiskFactor>>> GetRiskFactors(Guid transactionId)
    {
        var factors = await _riskScoringService.GetRiskFactorsAsync(transactionId);
        return Ok(factors);
    }

    [HttpGet("users/{userId}/trends")]
    public async Task<ActionResult<RiskTrends>> AnalyzeRiskTrends(string userId, [FromQuery] int days = 30)
    {
        var trends = await _riskScoringService.AnalyzeRiskTrendsAsync(userId, TimeSpan.FromDays(days));
        return Ok(trends);
    }
}
