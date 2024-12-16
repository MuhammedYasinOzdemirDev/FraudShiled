using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FraudShield.TransactionAnalysis.Infrastructure.Extensions;
public static class ValueComparers
{
    public static readonly ValueComparer<Dictionary<string, decimal>> FactorWeightsComparer =
        new ValueComparer<Dictionary<string, decimal>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(kv => kv.Key, kv => kv.Value));

    public static readonly ValueComparer<List<string>> IndicatorsComparer =
        new ValueComparer<List<string>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
}