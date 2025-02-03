using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FraudRuleConfiguration : IEntityTypeConfiguration<FraudRule>
{
    public void Configure(EntityTypeBuilder<FraudRule> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.RuleId);
        builder.Property(x => x.Name);
        builder.Property(x => x.Description);
        builder.Property(x => x.Condition);
        
        builder.Property(e => e.Action)
            .HasMaxLength(50)
            .HasConversion(
                v => v.ToString(),
                v => (RuleAction)Enum.Parse(typeof(RuleAction), v));
            
        builder.Property(e => e.Priority)
            .HasMaxLength(50)
            .HasConversion(
                v => v.ToString(),
                v => (RulePriority)Enum.Parse(typeof(RulePriority), v));
            
        builder.Property(x => x.Threshold)
            .HasColumnType("decimal(18,2)");
            
        builder.Property(x => x.IsActive);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.LastModifiedAt);
    }
}