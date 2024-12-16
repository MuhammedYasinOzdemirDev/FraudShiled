using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudShield.TransactionAnalysis.Infrastructure.Persistence.Configurations;

public class AnalysisRuleConfiguration : IEntityTypeConfiguration<AnalysisRule>
{
    public void Configure(EntityTypeBuilder<AnalysisRule> builder)
    {
        builder.ToTable("analysis_rules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.RiskImpact)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(e => e.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.Priority)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.LastTriggered);

        builder.Property(e => e.TriggerCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Configuration için JSON dönüşümü
        builder.Property(e => e.Configuration)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions
                {
                    WriteIndented = false
                }),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new Dictionary<string, string>()
            );

        // RuleCondition complex type mapping
        builder.OwnsOne(e => e.Condition, condition =>
        {
            condition.Property(c => c.Field)
                .HasColumnName("condition_field")
                .HasMaxLength(100)
                .IsRequired();

            condition.Property(c => c.Operator)
                .HasColumnName("condition_operator")
                .HasMaxLength(50)
                .IsRequired();

            condition.Property(c => c.Value)
                .HasColumnName("condition_value")
                .HasColumnType("jsonb");

            condition.Property(c => c.LogicalOperator)
                .HasColumnName("condition_logical_operator")
                .HasConversion<string>();

            condition.Property(c => c.SubConditions)
                .HasColumnName("sub_conditions")
                .HasColumnType("jsonb");
        });

        // Indexes
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Priority);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.LastTriggered);
        builder.HasIndex(e => new { e.Category, e.Priority, e.IsActive });
    }
}