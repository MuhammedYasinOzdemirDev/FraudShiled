using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
using FraudShield.TransactionAnalysis.Domain.Exceptions;

namespace FraudShield.TransactionAnalysis.Domain.Entities;

public class MLModel : AggregateRoot
{
 public string Name { get; private set; }
    public string Version { get; private set; }
    public ModelType Type { get; private set; }
    public string ModelPath { get; private set; }
    public IDictionary<string, string> Configuration { get; private set; }  // string olarak değiştirildi
    public ModelStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsed { get; private set; }
    public IDictionary<string, double> Metrics { get; private set; }
    public IDictionary<string, string> HyperParameters { get; private set; }  // object'ten string'e değiştirildi
    public string CreatedBy { get; private set; }

    private MLModel()
    {
        Metrics = new Dictionary<string, double>();
        Configuration = new Dictionary<string, string>();
        HyperParameters = new Dictionary<string, string>();
    }

    public static MLModel Create(
        string name,
        string version,
        ModelType type,
        string modelPath,
        string createdBy,
        IDictionary<string, string> configuration = null,
        IDictionary<string, string> hyperParameters = null)
    {
        var model = new MLModel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Version = version,
            Type = type,
            ModelPath = modelPath,
            Configuration = configuration ?? new Dictionary<string, string>(),
            Status = ModelStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            HyperParameters = hyperParameters ?? new Dictionary<string, string>()
        };

        model.AddDomainEvent(new MLModelCreatedDomainEvent(model));
        return model;
    }

    public void UpdateConfiguration(IDictionary<string, string> configuration)
    {
        Configuration = configuration;
    }

    public void UpdateHyperParameters(IDictionary<string, string> hyperParameters)
    {
        HyperParameters = hyperParameters;
    }
    public void Activate()
    {
        if (Status == ModelStatus.Failed)
            throw new InvalidModelStateException(Id, "Failed models cannot be activated");

        Status = ModelStatus.Active;
        AddDomainEvent(new MLModelActivatedDomainEvent(Id));
    }

    public void UpdateMetrics(IDictionary<string, double> metrics)
    {
        Metrics = metrics;
        LastUsed = DateTime.UtcNow;
        AddDomainEvent(new MLModelMetricsUpdatedDomainEvent(Id, metrics));
    }

    public void MarkAsFailed(string reason)
    {
        Status = ModelStatus.Failed;
        AddDomainEvent(new MLModelFailedDomainEvent(Id, reason));
    }
}