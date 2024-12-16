namespace FraudShield.TransactionAnalysis.Domain.Exceptions;

public class InvalidRiskProfileStateException : DomainException
{
    public string UserId { get; }
    public string Details { get; }

    public InvalidRiskProfileStateException(string userId, string message) 
        : base($"Risk Profile for user {userId}: {message}")
    {
        UserId = userId;
        Details = message;
    }
}