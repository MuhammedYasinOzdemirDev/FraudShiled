using Analiz.Application.Interfaces.ML;
using Analiz.Domain.Entities.ML;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Analiz.ML.Models.PCA;

public class PCAModelBuilder : IModelBuilder
{
    private readonly MLContext _mlContext;
    private readonly PCAConfiguration _configuration;

    public PCAModelBuilder(MLContext mlContext, PCAConfiguration configuration)
    {
        _mlContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IEstimator<ITransformer> BuildPipeline()
    {
        try
        {
            // Concatenate feature columns into a single "Features" column
            var featureColumns = _configuration.FeatureColumns.ToArray();
            var pipeline = _mlContext.Transforms
                .Concatenate("Features", featureColumns)

                // Normalize features (Min-Max scaling)
                .Append(_mlContext.Transforms.NormalizeMinMax(
                    outputColumnName: "NormalizedFeatures",
                    inputColumnName: "Features"))

                // Apply PCA to normalized features
                .Append(_mlContext.Transforms.ProjectToPrincipalComponents(
                    outputColumnName: "PCAFeatures",
                    inputColumnName: "NormalizedFeatures",
                    rank: _configuration.ComponentCount));

            // Add a Randomized PCA anomaly detection trainer
            var randomizedPcaTrainer = _mlContext.AnomalyDetection.Trainers
                .RandomizedPca(
                    featureColumnName: "PCAFeatures",
                    rank: _configuration.ComponentCount,
                    ensureZeroMean: _configuration.StandardizeInput);

            return pipeline.Append(randomizedPcaTrainer);
        }
        catch (Exception ex)
        {
            throw new Exception("Error building PCA pipeline", ex);
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
            throw new Exception("Error training PCA model", ex);
        }
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

    public ModelOutput Predict(IDataView data, ITransformer model)
    {
        try
        {
            var transformedData = model.Transform(data);

            var predictions = _mlContext.Data
                .CreateEnumerable<PCAModelOutput>(transformedData, reuseRowObject: false);

            var prediction = predictions.First();

            return new ModelOutput
            {
                PredictedLabel = prediction.AnomalyScore > _configuration.AnomalyThreshold,
                Score = (float)prediction.AnomalyScore,
                Probability = 1f - ((float)prediction.AnomalyScore / ((float)_configuration.AnomalyThreshold * 2f))
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error making prediction with PCA model", ex);
        }
    }

    public double CalculateExplainedVariance(ITransformer model, IDataView data)
    {
        try
        {
            var transformedData = model.Transform(data);
            var pcaFeatures = _mlContext.Data
                .CreateEnumerable<PCAModelOutput>(transformedData, reuseRowObject: false)
                .Select(x => x.PCAFeatures)
                .ToList();

            if (!pcaFeatures.Any())
                return 0;

            var totalVariance = pcaFeatures.Sum(f => f.Sum(x => x * x));
            var explainedVariance = pcaFeatures
                .Take(_configuration.ComponentCount)
                .Sum(f => f.Sum(x => x * x));

            return totalVariance > 0 ? explainedVariance / totalVariance : 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Error calculating explained variance", ex);
        }
    }
}