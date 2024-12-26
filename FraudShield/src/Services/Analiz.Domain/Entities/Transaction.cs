using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Transactions;
using Analiz.Domain.Events;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using TransactionStatus = FraudShield.TransactionAnalysis.Domain.Enums.TransactionStatus;

namespace Analiz.Domain.Entities;


public class Transaction : Entity
{
    public string UserId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime TransactionTime { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionDetails Details { get; private set; }
    public RiskScore RiskScore { get; private set; }
    
    [NotMapped]
    private readonly List<TransactionFlag> _flags = new();
    
    [NotMapped]
    public IReadOnlyCollection<TransactionFlag> Flags => _flags.AsReadOnly();
    
    public string FlagsJson
    {
        get => _flags != null ? JsonSerializer.Serialize(_flags) : null;
        private set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _flags.Clear();
                _flags.AddRange(JsonSerializer.Deserialize<List<TransactionFlag>>(value));
            }
        }
    }

    public string MerchantId { get; private set; }
    public DeviceInfo DeviceInfo { get; private set; }
    public Location Location { get; private set; }

    private Transaction()
    {
        _flags = new List<TransactionFlag>();
    }

    public static Transaction Create(
        string userId,
        decimal amount,
        TransactionType type,
        string merchantId,
        TransactionDetails details,
        DeviceInfo deviceInfo,
        Location location)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            TransactionTime = DateTime.UtcNow,
            Type = type,
            Status = TransactionStatus.Pending,
            MerchantId = merchantId,
            Details = details,
            DeviceInfo = deviceInfo,
            Location = location
        };

        transaction.FlagsJson = JsonSerializer.Serialize(new List<TransactionFlag>());
        return transaction;
    }

    public void SetRiskScore(RiskScore score)
    {
        RiskScore = score;
        Status = DetermineStatus(score);
        AddDomainEvent(new TransactionRiskScoreUpdatedEvent(Id, score));
    }

    public void UpdateStatus(TransactionStatus newStatus)
    {
        if (Status != newStatus)
        {
            Status = newStatus;
            AddDomainEvent(new TransactionStatusUpdatedEvent(Id, newStatus));
        }
    }

    public void AddFlag(TransactionFlag flag)
    {
        _flags.Add(flag);
        FlagsJson = JsonSerializer.Serialize(_flags);
    }

    public void RemoveFlag(TransactionFlag flag)
    {
        _flags.Remove(flag);
        FlagsJson = JsonSerializer.Serialize(_flags);
    }

    public void ClearFlags()
    {
        _flags.Clear();
        FlagsJson = JsonSerializer.Serialize(_flags);
    }

    private TransactionStatus DetermineStatus(RiskScore score) =>
        score.Level switch
        {
            RiskLevel.Critical => TransactionStatus.Blocked,
            RiskLevel.High => TransactionStatus.RequiresReview,
            _ => TransactionStatus.Approved
        };
}
