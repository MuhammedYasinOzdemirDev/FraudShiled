using System.Text.Json;
using Analiz.Domain.Entities;
using Analiz.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId);
        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");
        builder.Property(x => x.TransactionTime);
        
        builder.Property(x => x.Type)
            .HasConversion<string>();
            
        builder.Property(x => x.Status)
            .HasConversion<string>();

        // Store flags as JSON
        builder.Property(x => x.FlagsJson)
            .HasColumnType("jsonb");
            
        builder.Property(x => x.Details)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<TransactionDetails>(v, JsonSerializerOptions.Default));

        builder.OwnsOne(x => x.RiskScore, rs =>
        {
            rs.Property(r => r.Score).HasColumnName("RiskScore");
            rs.Property(r => r.Level)
                .HasColumnName("RiskLevel")
                .HasConversion<string>();
        });

        builder.Property(x => x.DeviceInfo)
            .HasColumnType("jsonb");

        builder.Property(x => x.Location)
            .HasColumnType("jsonb");

        builder.Ignore(x => x.Flags);
    }
}