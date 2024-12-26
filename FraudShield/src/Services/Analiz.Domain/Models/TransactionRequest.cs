namespace Analiz.Domain.Entities;

public class TransactionRequest
{
    public Guid TransactionId { get; set; }
    public TransactionData TransactionData { get; set; }
}
