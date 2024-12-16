namespace FraudShield.TransactionAnalysis.Domain.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = new List<string>();
    }

    protected Result(bool isSuccess, T value, List<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors ?? new List<string>();
        Error = string.Join(", ", Errors);
    }

    public static Result<T> Success(T value) => new(true, value, error: null);

    public static Result<T> Failure(string error) => new(false, default, error);

    public static Result<T> Failure(List<string> errors) => new(false, default, errors);

    public bool IsFailure => !IsSuccess;
}
