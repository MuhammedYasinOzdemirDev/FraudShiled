using FraudShield.TransactionAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudShield.TransactionAnalysis.Infrastructure.Persistence.Configurations;

public class AnalysisSessionConfiguration : IEntityTypeConfiguration<AnalysisSession>
{
    public void Configure(EntityTypeBuilder<AnalysisSession> builder)
    {
        builder.ToTable("analysis_sessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AnalysisId)
            .IsRequired();

        builder.Property(e => e.SessionId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.EndTime);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.ModelVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.SessionData)
            .HasColumnType("jsonb");

        // Analysis Steps collection
        builder.Property(e => e.Steps)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(e => e.SessionId).IsUnique();
        builder.HasIndex(e => e.AnalysisId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartTime);
    }
}
