using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudShield.TransactionAnalysis.Infrastructure.Persistence.Configurations;

public class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
     public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("analyses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransactionId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ModelVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.AnalyzedAt)
            .IsRequired();

        builder.Property(e => e.CompletedAt);

        // Metadata konfigürasyonu
        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        // AnalysisMetrics konfigürasyonu
        builder.OwnsOne(e => e.Metrics, metrics =>
        {
            metrics.Property(m => m.AnomalyScore)
                .HasColumnName("anomaly_score")
                .HasColumnType("decimal(5,2)");

            metrics.Property(m => m.PatternMatchScore)
                .HasColumnName("pattern_match_score")
                .HasColumnType("decimal(5,2)");

            metrics.Property(m => m.BehaviorScore)
                .HasColumnName("behavior_score")
                .HasColumnType("decimal(5,2)");

            metrics.Property(m => m.VelocityScore)
                .HasColumnName("velocity_score")
                .HasColumnType("decimal(5,2)");

            metrics.Property(m => m.FeatureImportance)
                .HasColumnName("feature_importance")
                .HasColumnType("jsonb");

            metrics.Property(m => m.DetectedPatterns)
                .HasColumnName("detected_patterns")
                .HasColumnType("jsonb");

            metrics.Property(m => m.AdditionalMetricsJson)
                .HasColumnName("additional_metrics")
                .HasColumnType("jsonb");

            metrics.Property(m => m.CalculatedAt)
                .HasColumnName("calculated_at");
        });
        builder.OwnsOne(a => a.RiskScore, rs =>
        {
            rs.Property(r => r.Score)
                .IsRequired()
                .HasColumnName("RiskScore");

            rs.Property(r => r.Level)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnName("RiskLevel");

            rs.Property(r => r.CalculatedAt)
                .HasColumnName("CalculatedAt");

            rs.Property(r => r.Indicators)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
                    v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<string>())
                .HasColumnType("jsonb")
                .HasColumnName("Indicators")
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            rs.Property(r => r.FactorWeights)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Dictionary<string, decimal>())
                .HasColumnType("jsonb")
                .HasColumnName("FactorWeights")
                .Metadata.SetValueComparer(new ValueComparer<IDictionary<string, decimal>>(
                    (d1, d2) => d1 != null && d2 != null && d1.OrderBy(kv => kv.Key).SequenceEqual(d2.OrderBy(kv => kv.Key)),
                    d => d != null ? d.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())) : 0,
                    d => d != null ? new Dictionary<string, decimal>(d) : new Dictionary<string, decimal>()));
        });
        
        // Indexes
        builder.HasIndex(e => e.TransactionId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.AnalyzedAt);
    }
}