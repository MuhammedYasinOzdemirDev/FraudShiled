using System.Collections.Concurrent;
using Analiz.Application.Interfaces;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

public class FraudRuleEngine : IFraudRuleEngine
    {
        private readonly IFraudRuleRepository _ruleRepository;
        private readonly ILogger<FraudRuleEngine> _logger;
        private readonly RuleCompiler _ruleCompiler;
        
        // In-memory cache of compiled rules
        private ConcurrentDictionary<string, Func<TransactionData, bool>> _compiledRules;
        private Dictionary<string, FraudRule> _ruleMetadata;
        
        public FraudRuleEngine(
            IFraudRuleRepository ruleRepository,
            ILogger<FraudRuleEngine> logger)
        {
            _ruleRepository = ruleRepository;
            _logger = logger;
            _ruleCompiler = new RuleCompiler();
            _compiledRules = new ConcurrentDictionary<string, Func<TransactionData, bool>>();
            _ruleMetadata = new Dictionary<string, FraudRule>();
            
            // Initialize rules async
            Task.Run(ReloadRulesAsync).GetAwaiter().GetResult();
        }

        public async Task<IEnumerable<RuleResult>> EvaluateRulesAsync(TransactionData data)
        {
            var results = new List<RuleResult>();
            
            foreach (var rule in _ruleMetadata.Values.Where(r => r.IsActive))
            {
                try
                {
                    // Get the compiled rule function
                    if (_compiledRules.TryGetValue(rule.RuleId, out var ruleFunc))
                    {
                        bool isTriggered = ruleFunc(data);
                        
                        results.Add(new RuleResult
                        {
                            RuleId = rule.RuleId,
                            RuleName = rule.Name,
                            IsTriggered = isTriggered,
                            Action = rule.Action,
                            Confidence = isTriggered ? 1.0 : 0.0 // Rule confidence is binary
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating rule {RuleId}", rule.RuleId);
                }
            }
            
            // Sort by priority
            return results.OrderBy(r => _ruleMetadata[r.RuleId].Priority);
        }

        public async Task ReloadRulesAsync()
        {
            try
            {
                _logger.LogInformation("Reloading fraud rules");
                
                // Fetch all active rules
                var rules = await _ruleRepository.GetActiveRulesAsync();
                
                // Clear existing rules
                _compiledRules.Clear();
                _ruleMetadata.Clear();
                
                // Compile and cache rules
                foreach (var rule in rules)
                {
                    try
                    {
                        var compiledRule = _ruleCompiler.CompileRule(rule.Condition);
                        
                        _compiledRules[rule.RuleId] = compiledRule;
                        _ruleMetadata[rule.RuleId] = rule;
                        
                        _logger.LogInformation("Loaded rule {RuleId}: {RuleName}", rule.RuleId, rule.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error compiling rule {RuleId}: {RuleName}", rule.RuleId, rule.Name);
                    }
                }
                
                _logger.LogInformation("Successfully loaded {Count} fraud rules", _compiledRules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading fraud rules");
                throw;
            }
        }
    }

    /// <summary>
    /// Compiles string rule expressions into executable functions
    /// </summary>
    public class RuleCompiler
    {
        public Func<TransactionData, bool> CompileRule(string ruleExpression)
        {
            // This is a simplified implementation
            // In a real implementation, you would parse the rule expression
            // and compile it to a lambda expression
            
            // For demonstration, let's parse some simple rules
            
            // Example: "Amount > 1000"
            if (ruleExpression.Contains("Amount >"))
            {
                decimal threshold = decimal.Parse(ruleExpression.Split('>')[1].Trim());
                return data => data.Amount > threshold;
            }
            
            // Example: "Country IN ('RU', 'KP')"
            if (ruleExpression.Contains("Country IN"))
            {
                var countries = ruleExpression
                    .Split('(')[1]
                    .Split(')')[0]
                    .Split(',')
                    .Select(c => c.Trim().Trim('\''))
                    .ToList();
                    
                return data => data.Location != null && countries.Contains(data.Location.Country);
            }
            
            // Example: "DeviceInfo.IpChanged = true"
            if (ruleExpression.Contains("DeviceInfo.IpChanged"))
            {
                return data => data.DeviceInfo != null && data.DeviceInfo.IpChanged;
            }
            
            // Example: "TransactionType = 'HighRisk'"
            if (ruleExpression.Contains("TransactionType ="))
            {
                var typeStr = ruleExpression.Split('=')[1].Trim().Trim('\'');
                TransactionType type = Enum.Parse<TransactionType>(typeStr);
                return data => data.Type == type;
            }
            
            // Default fallback rule (always false)
            return _ => false;
        }
    }