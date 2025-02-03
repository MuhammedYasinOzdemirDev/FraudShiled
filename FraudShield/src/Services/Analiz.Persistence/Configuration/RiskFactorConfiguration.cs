using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class RiskFactorConfiguration : IEntityTypeConfiguration<RiskFactor>
{
    public void Configure(EntityTypeBuilder<RiskFactor> builder)
    {
        builder.HasKey(x => x.Code);
        
        builder.Property(x => x.Description);
        builder.Property(x => x.Weight);
        builder.Property(x => x.Severity);
    }
}