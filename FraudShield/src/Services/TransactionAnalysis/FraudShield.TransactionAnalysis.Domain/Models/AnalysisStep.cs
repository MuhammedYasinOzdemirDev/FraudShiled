using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class AnalysisStep
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public StepStatus Status { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public TimeSpan? Duration => EndTime - StartTime;
    public IDictionary<string, object> StepData { get; private set; }

    public AnalysisStep(string name, string description, StepStatus status)
    {
        Name = name;
        Description = description;
        Status = status;
        StartTime = DateTime.UtcNow;
        StepData = new Dictionary<string, object>();
    }
}