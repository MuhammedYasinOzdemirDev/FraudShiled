using FraudShield.TransactionAnalysis.ML.Models.Common.Enums;

namespace FraudShield.TransactionAnalysis.ML.Models.Common;
public abstract class ModelBase : IModelBase
{
    public Guid Id { get; protected set; }
    public string Name { get; protected set; }
    public string Version { get; protected set; }
    public ModelType Type { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public ModelStatus Status { get; protected set; }
    public IDictionary<string, double> Metrics { get; protected set; }

    protected ModelBase()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        Status = ModelStatus.Draft;
        Metrics = new Dictionary<string, double>();
    }
}