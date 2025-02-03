using System.Globalization;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML.DataSet;
using Microsoft.ML;

namespace Analiz.Application.Converter;

public static class CreditCardMLConverter
{
  public static IDataView ConvertToMLDataView(MLContext mlContext, List<CreditCardModelData> data)
{
    var mlData = data.Select(x => new CreditCardMLData
    {
        Time = x.Time,
        V1 = x.V1,
        V2 = x.V2,
        V3 = x.V3,
        V4 = x.V4,
        V5 = x.V5,
        V6 = x.V6,
        V7 = x.V7,
        V8 = x.V8,
        V9 = x.V9,
        V10 = x.V10,
        V11 = x.V11,
        V12 = x.V12,
        V13 = x.V13,
        V14 = x.V14,
        V15 = x.V15,
        V16 = x.V16,
        V17 = x.V17,
        V18 = x.V18,
        V19 = x.V19,
        V20 = x.V20,
        V21 = x.V21,
        V22 = x.V22,
        V23 = x.V23,
        V24 = x.V24,
        V25 = x.V25,
        V26 = x.V26,
        V27 = x.V27,
        V28 = x.V28,
        Amount = x.Amount,
        Label = x.Label
    }).ToList();

    var dataView = mlContext.Data.LoadFromEnumerable(mlData);

    // Feature transformation pipeline
    var pipeline = mlContext.Transforms
        // Time feature transformation
        .CustomMapping(
            (CreditCardMLData input, TimeFeatures output) =>
            {
                const double daySeconds = 24 * 60 * 60;
                output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / daySeconds);
                output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / daySeconds);
            },
            "TimeFeatureMapping")
        // Amount feature transformation
        .Append(mlContext.Transforms.CustomMapping(
            (CreditCardMLData input, AmountFeatures output) =>
            {
                output.LogAmount = (float)Math.Log(input.Amount + 1);
            },
            "AmountFeatureMapping"))
        // Combine features
        .Append(mlContext.Transforms.Concatenate("Features",
            new[]
            {
                "TimeSin", "TimeCos", "LogAmount",
                "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
                "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
                "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
            }));

    return pipeline.Fit(dataView).Transform(dataView);
}


public static List<TransactionData> ToTransactionDataList(IDataView dataView, MLContext mlContext)
{
    var data = mlContext.Data.CreateEnumerable<CreditCardMLData>(dataView, reuseRowObject: false);

    return data.Select(x => new TransactionData
    {
        TransactionId = Guid.NewGuid(),
        Amount = Convert.ToDecimal(x.Amount),
        Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)x.Time).DateTime,
        IsFraudulent = x.Label,
        AdditionalData = new Dictionary<string, string>
        {
       ["V1"] = x.V1.ToString(CultureInfo.InvariantCulture),
                   ["V2"] = x.V2.ToString(CultureInfo.InvariantCulture),
                   ["V3"] = x.V3.ToString(CultureInfo.InvariantCulture),
                   ["V4"] = x.V4.ToString(CultureInfo.InvariantCulture),
                   ["V5"] = x.V5.ToString(CultureInfo.InvariantCulture),
                   ["V6"] = x.V6.ToString(CultureInfo.InvariantCulture),
                   ["V7"] = x.V7.ToString(CultureInfo.InvariantCulture),
                   ["V8"] = x.V8.ToString(CultureInfo.InvariantCulture),
                   ["V9"] = x.V9.ToString(CultureInfo.InvariantCulture),
                   ["V10"] = x.V10.ToString(CultureInfo.InvariantCulture),
                   ["V11"] = x.V11.ToString(CultureInfo.InvariantCulture),
                   ["V12"] = x.V12.ToString(CultureInfo.InvariantCulture),
                   ["V13"] = x.V13.ToString(CultureInfo.InvariantCulture),
                   ["V14"] = x.V14.ToString(CultureInfo.InvariantCulture),
                   ["V15"] = x.V15.ToString(CultureInfo.InvariantCulture),
                   ["V16"] = x.V16.ToString(CultureInfo.InvariantCulture),
                   ["V17"] = x.V17.ToString(CultureInfo.InvariantCulture),
                   ["V18"] = x.V18.ToString(CultureInfo.InvariantCulture),
                   ["V19"] = x.V19.ToString(CultureInfo.InvariantCulture),
                   ["V20"] = x.V20.ToString(CultureInfo.InvariantCulture),
                   ["V21"] = x.V21.ToString(CultureInfo.InvariantCulture),
                   ["V22"] = x.V22.ToString(CultureInfo.InvariantCulture),
                   ["V23"] = x.V23.ToString(CultureInfo.InvariantCulture),
                   ["V24"] = x.V24.ToString(CultureInfo.InvariantCulture),
                   ["V25"] = x.V25.ToString(CultureInfo.InvariantCulture),
                   ["V26"] = x.V26.ToString(CultureInfo.InvariantCulture),
                   ["V27"] = x.V27.ToString(CultureInfo.InvariantCulture),
                   ["V28"] = x.V28.ToString(CultureInfo.InvariantCulture)
        }
    }).ToList();
    }
}