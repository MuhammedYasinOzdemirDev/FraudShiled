using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces;

public interface IFraudRuleEngine
{
    Task<IEnumerable<RuleResult>> EvaluateRulesAsync(TransactionData data);
    Task ReloadRulesAsync();
}