namespace FraudShield.TransactionAnalysis.Domain.Exceptions;

public class ModelTrainingException : DomainException
{
    public string ModelName { get; }
    public string ModelVersion { get; }
    public string Details { get; }

    public ModelTrainingException(string modelName, string modelVersion, string message) 
        : base($"Model Training failed for {modelName} version {modelVersion}: {message}")
    {
        ModelName = modelName;
        ModelVersion = modelVersion;
        Details = message;
    }
}