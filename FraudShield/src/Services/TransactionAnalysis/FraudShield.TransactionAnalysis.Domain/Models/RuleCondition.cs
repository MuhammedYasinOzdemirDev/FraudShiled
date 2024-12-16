using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class RuleCondition
{
    public string Field { get; private set; }
    public string Operator { get; private set; }
    public object Value { get; private set; }
    public List<RuleCondition> SubConditions { get; private set; }
    public LogicalOperator LogicalOperator { get; private set; }

    private RuleCondition()
    {
        SubConditions = new List<RuleCondition>();
    }

    public static RuleCondition Create(
        string field,
        string @operator,
        object value,
        LogicalOperator logicalOperator = LogicalOperator.And)
    {
        return new RuleCondition
        {
            Field = field,
            Operator = @operator,
            Value = value,
            LogicalOperator = logicalOperator
        };
    }
}
