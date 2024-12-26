using Analiz.Application.Interfaces.ML;
using Analiz.Domain.Entities.ML;
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
            var featureColumns = _configuration.FeatureColumns.ToArray();
            var categoricalColumns = _configuration.FeatureColumns
                .Where(IsCategoricalColumn)
                .ToArray();

            // Create a list to hold all feature column names (both original and encoded)
            var allFeatureColumns = new List<string>();
            
            // Add non-categorical columns directly
            allFeatureColumns.AddRange(featureColumns.Except(categoricalColumns));

            // Start building pipeline
            IEstimator<ITransformer> pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label");

            // Add one-hot encoding for categorical columns
            if (categoricalColumns.Any())
            {
                foreach (var column in categoricalColumns)
                {
                    var encodedName = $"{column}_encoded";
                    allFeatureColumns.Add(encodedName);
                    
                    pipeline = pipeline.Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                        outputColumnName: encodedName,
                        inputColumnName: column));
                }
            }

            // Concatenate all features into a single column
            pipeline = pipeline.Append(_mlContext.Transforms.Concatenate("Features", allFeatureColumns.ToArray()));

            // Apply normalization to the concatenated features
            pipeline = pipeline.Append(_mlContext.Transforms.NormalizeMinMax(
                outputColumnName: "NormalizedFeatures",
                inputColumnName: "Features"));

            // Add FastTree trainer
            var trainer = _mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "NormalizedFeatures",
                numberOfLeaves: _configuration.NumberOfLeaves,
                numberOfTrees: _configuration.NumberOfTrees,
                minimumExampleCountPerLeaf: _configuration.MinDataInLeaf,
                learningRate: (float)_configuration.LearningRate);

            return pipeline.Append(trainer);
        }
        catch (Exception ex)
        {
            throw new Exception("Error building FastTree pipeline", ex);
        }
    }

    private bool IsCategoricalColumn(string columnName)
    {
        return columnName.StartsWith("Category_") || 
               columnName.EndsWith("_Type") || 
               columnName.Contains("_Id");
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
            throw new Exception("Error training FastTree model", ex);
        }
    }

    public ModelOutput Predict(IDataView data, ITransformer model)
    {
        try
        {
            var transformedData = model.Transform(data);
            var predictions = _mlContext.Data
                .CreateEnumerable<FastTreePrediction>(transformedData, reuseRowObject: false)
                .First();

            return new ModelOutput
            {
                PredictedLabel = predictions.PredictedLabel,
                Score = predictions.Score,
                Probability = predictions.Probability
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error making prediction", ex);
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
}