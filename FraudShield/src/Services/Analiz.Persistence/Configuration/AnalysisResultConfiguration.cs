using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
{
   
        public void Configure(EntityTypeBuilder<AnalysisResult> builder)
        {
            builder.HasKey(x => x.Id);
        
            builder.Property(x => x.TransactionId);
            builder.Property(x => x.AnomalyScore);
            builder.Property(x => x.FraudProbability);
        
            builder.OwnsOne(x => x.RiskScore, rs =>
            {
                rs.Property(r => r.Score).HasColumnName("RiskScore");
                rs.Property(r => r.Level)
                    .HasColumnName("RiskLevel")
                    .HasConversion<string>();
            });
        
            builder.HasMany(x => x.RiskFactors)
                .WithOne()
                .IsRequired(false);
        
            builder.Property(x => x.Decision)
                .HasConversion<string>();
            
            builder.Property(x => x.Status)
                .HasConversion<string>();
        }
    

}