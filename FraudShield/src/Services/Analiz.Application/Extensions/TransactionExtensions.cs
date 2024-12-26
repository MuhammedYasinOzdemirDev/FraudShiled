using Analiz.Domain.Entities;

namespace Analiz.Application.Extensions;

public static class TransactionExtensions
{
    public static TransactionData ToTransactionData(this Transaction transaction)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        return new TransactionData
        {
            TransactionId = transaction.Id,
            UserId = transaction.UserId,
            Amount = transaction.Amount,
            Type = transaction.Type,
            Timestamp = transaction.TransactionTime,
            Location = transaction.Location,
            DeviceInfo = transaction.DeviceInfo,
            MerchantId = transaction.MerchantId
        };
    }
}
