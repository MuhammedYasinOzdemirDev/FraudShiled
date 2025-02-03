using System.Text.Json;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FraudAlertConfiguration : IEntityTypeConfiguration<FraudAlert>
{
    public void Configure(EntityTypeBuilder<FraudAlert> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.TransactionId);
        builder.Property(x => x.UserId);
        
        builder.Property(x => x.Type)
            .HasConversion<string>();
            
        builder.Property(x => x.Status)
            .HasConversion<string>();
            
        builder.OwnsOne(x => x.RiskScore, rs =>
        {
            rs.Property(r => r.Score).HasColumnName("RiskScore");
            rs.Property(r => r.Level)
                .HasColumnName("RiskLevel")
                .HasConversion<string>();
        });
        
        builder.Property(x => x.Factors)
            .HasColumnType("jsonb");
            
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.ResolvedAt);
        builder.Property(x => x.Resolution);
    }
}