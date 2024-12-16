using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Infrastructure.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FraudShield.TransactionAnalysis.Infrastructure.Extensions;

public static class ModelBuilderExtensions
{
    public static void ConfigureRiskFactors(this ModelBuilder modelBuilder)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new RiskFactorJsonConverter());

        var riskFactorListConverter = new ValueConverter<List<RiskFactor>, string>(
            v => JsonSerializer.Serialize(v, options),
            v => JsonSerializer.Deserialize<List<RiskFactor>>(v, options) ?? new List<RiskFactor>()
        );

        modelBuilder.Entity<Analysis>()
            .Property(e => e.RiskFactors)
            .HasConversion(riskFactorListConverter);
    }
}
