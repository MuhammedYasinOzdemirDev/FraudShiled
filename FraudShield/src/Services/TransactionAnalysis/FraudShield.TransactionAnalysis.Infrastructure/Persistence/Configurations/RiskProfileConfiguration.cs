using FraudShield.TransactionAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudShield.TransactionAnalysis.Infrastructure.Persistence.Configurations;

public class RiskProfileConfiguration : IEntityTypeConfiguration<RiskProfile>
{
    public void Configure(EntityTypeBuilder<RiskProfile> builder)
    {
        builder.ToTable("risk_profiles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.BaseRiskScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(e => e.LastUpdated)
            .IsRequired();

        builder.Property(e => e.CurrentRiskLevel)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.RiskMetrics)
            .HasColumnType("jsonb");

        builder.Property(e => e.UserAttributes)
            .HasColumnType("jsonb");

        // Historical Factors collection
        builder.Property(e => e.HistoricalFactors)
            .HasColumnType("jsonb");

        // Velocity Rules collection
        builder.Property(e => e.VelocityRules)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(e => e.UserId).IsUnique();
        builder.HasIndex(e => e.CurrentRiskLevel);
        builder.HasIndex(e => e.LastUpdated);
        builder.HasIndex(e => e.IsActive);
    }
}