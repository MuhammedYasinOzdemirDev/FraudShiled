using FraudShield.TransactionAnalysis.Domain.Models;

namespace FraudShield.TransactionAnalysis.Domain.Exceptions;

public class DataValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public DataValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Data validation failed with {errors.Count} errors")
    {
        Errors = errors;
    }

    public DataValidationException(string message) : base(message)
    {
        Errors = Array.Empty<ValidationError>();
    }
}