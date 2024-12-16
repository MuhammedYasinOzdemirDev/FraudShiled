using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudShield.TransactionAnalysis.Infrastructure.Persistence.Configurations;

public class MLModelConfiguration : IEntityTypeConfiguration<MLModel>
{
    public void Configure(EntityTypeBuilder<MLModel> builder)
    {
        builder.ToTable("ml_models");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.ModelPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.LastUsed);

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        // Configuration as jsonb
        builder.Property(e => e.Configuration)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default) 
                     ?? new Dictionary<string, string>());

        // Metrics as jsonb
        builder.Property(e => e.Metrics)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonSerializerOptions.Default) 
                     ?? new Dictionary<string, double>());

        // HyperParameters as jsonb
        builder.Property(e => e.HyperParameters)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonSerializerOptions.Default) 
                     ?? new Dictionary<string, string>());

        // Indexes
        builder.HasIndex(e => new { e.Name, e.Version }).IsUnique();
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CreatedAt);
    }
}