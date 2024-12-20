using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.ML.Extensions;

public static class DataViewExtensions
{
    public static long? GetRowCount(this IDataView dataView)
    {
        return dataView.GetRowCount();
    }

    public static bool HasColumn(this IDataView dataView, string columnName)
    {
        return dataView.Schema.GetColumnOrNull(columnName) != null;
    }
}