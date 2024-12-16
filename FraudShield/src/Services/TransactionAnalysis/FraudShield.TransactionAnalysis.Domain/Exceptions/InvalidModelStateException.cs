namespace FraudShield.TransactionAnalysis.Domain.Exceptions;

public class InvalidModelStateException : DomainException
{
    public Guid ModelId { get; }
    public string Details { get; }
    
    public InvalidModelStateException(Guid modelId, string message) 
        : base($"ML Model {modelId}: {message}")
    {
        ModelId = modelId;
        Details = message;
    }
}