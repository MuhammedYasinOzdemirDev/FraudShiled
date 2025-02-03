using Analiz.Application.Interfaces.ML;
using Analiz.Application.Transform;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
using Analiz.Domain.Entities.ML.Transaction;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace Analiz.ML.Models.LightGBM;

public class LightGBMModelBuilder : IModelBuilder
{
    private readonly MLContext _mlContext;
    private readonly LightGBMConfiguration _configuration;


    public LightGBMModelBuilder(MLContext mlContext, LightGBMConfiguration configuration)
    {
        _mlContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
public IEstimator<ITransformer> BuildPipeline()
    {
        try
        {
            var transformations = new List<IEstimator<ITransformer>>();
            var featureColumns = new List<string>();

            // 1. Time features
            transformations.Add(_mlContext.Transforms.CustomMapping<CreditCardMLData, TimeFeatures>(
                (input, output) =>
                {
                    output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / _configuration.TimeScaleFactor);
                    output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / _configuration.TimeScaleFactor);
                    output.DayFeature = (float)(input.Time / (24 * 3600)) % 7;
                    output.HourFeature = (float)(input.Time / 3600) % 24;
                },
                "TimeFeatureMapping"));
            featureColumns.AddRange(new[] { "TimeSin", "TimeCos", "DayFeature", "HourFeature" });

            // 2. Amount features
            transformations.Add(_mlContext.Transforms.CustomMapping<CreditCardMLData, AmountFeatures>(
                (input, output) =>
                {
                    output.Amount = input.Amount;
                    output.Amount_normalized = (float)((input.Amount - _configuration.MinAmount) / 
                        (_configuration.MaxAmount - _configuration.MinAmount));
                    output.Amount_log = (float)Math.Log(input.Amount + 1);
                    output.LogAmount = output.Amount_log;
                },
                "AmountFeatureMapping"));
            featureColumns.AddRange(new[] { "Amount", "Amount_normalized", "LogAmount" });

            // 3. V1-V28 features normalization
            for (int i = 1; i <= 28; i++)
            {
                transformations.Add(_mlContext.Transforms.NormalizeMeanVariance(
                    outputColumnName: $"V{i}_normalized",
                    inputColumnName: $"V{i}",
                    fixZero: true));
                featureColumns.Add($"V{i}_normalized");
            }

            // 4. Add sample weights if enabled
            if (_configuration.UseClassWeights)
            {
                transformations.Add(_mlContext.Transforms.Conversion.MapValue(
                    outputColumnName: "SampleWeight",
                    inputColumnName: "Label",
                    keyValuePairs: new[]
                    {
                        new KeyValuePair<bool, float>(false, (float)_configuration.ClassWeights["0"]),
                        new KeyValuePair<bool, float>(true, (float)_configuration.ClassWeights["1"])
                    }));
            }

            // 5. Feature concatenation
            transformations.Add(_mlContext.Transforms.Concatenate("Features", featureColumns.ToArray()));

            // 6. FastTree trainer configuration
            var trainerOptions = new FastTreeBinaryTrainer.Options
            {
                NumberOfLeaves = _configuration.NumberOfLeaves,
                MinimumExampleCountPerLeaf = _configuration.MinDataInLeaf,
                LearningRate = (float)_configuration.LearningRate,
                NumberOfTrees = _configuration.NumberOfTrees,
                FeatureFraction = (float)_configuration.FeatureFraction,
                LabelColumnName = "Label",
                FeatureColumnName = "Features"
            };

            // Add weight column to trainer if enabled
            if (_configuration.UseClassWeights)
            {
                trainerOptions.ExampleWeightColumnName = "SampleWeight";
            }

            // Add the trainer
            transformations.Add(_mlContext.BinaryClassification.Trainers.FastTree(trainerOptions));

            // Build final pipeline
            IEstimator<ITransformer> pipeline = transformations[0];
            for (int i = 1; i < transformations.Count; i++)
            {
                pipeline = pipeline.Append(transformations[i]);
            }

            return pipeline;
        }
        catch (Exception ex)
        {
            throw new Exception("Error building LightGBM pipeline", ex);
        }
    }


    public ITransformer Train(IDataView trainingData)
    {
        try
        {
            var pipeline = BuildPipeline();
            return pipeline.Fit(trainingData);
        }
        catch (Exception ex)
        {
            throw new Exception("Error training model", ex);
        }
    }

    public ModelOutput Predict(IDataView data, ITransformer model)
    {
        try
        {
            var transformedData = model.Transform(data);
            var prediction = _mlContext.Data
                .CreateEnumerable<LightGBMOutput>(transformedData, reuseRowObject: false)
                .First();

            // Calculate feature importance
            var featureImportances = CalculateFeatureImportance(model, data);

            return new ModelOutput
            {
                PredictedLabel = prediction.PredictedLabel,
                Score = prediction.Score,
                Probability = prediction.Probability,
                Metadata = new Dictionary<string, object>
                {
                    ["ConfidenceScore"] = prediction.ConfidenceScore,
                    ["UncertaintyScore"] = prediction.UncertaintyScore,
                    ["TopFeatures"] = string.Join(",", prediction.TopContributingFeatures)
                }
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private Dictionary<string, double> CalculateFeatureImportance(ITransformer model, IDataView data)
    {
        // Feature importance hesaplama mantığı
        return new Dictionary<string, double>();
    }

    public void SaveModel(ITransformer model, string modelPath)
    {
        try
        {
            _mlContext.Model.Save(model, null, modelPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving model to path: {modelPath}", ex);
        }
    }

    public ITransformer LoadModel(string modelPath)
    {
        try
        {
            return _mlContext.Model.Load(modelPath, out _);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading model from path: {modelPath}", ex);
        }
    }
}