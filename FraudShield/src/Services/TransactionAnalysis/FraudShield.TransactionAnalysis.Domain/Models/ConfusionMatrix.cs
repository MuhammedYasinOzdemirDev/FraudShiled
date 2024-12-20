namespace FraudShield.TransactionAnalysis.Domain.Models;
// Domain/Models/ConfusionMatrix.cs
public class ConfusionMatrix
{
    public double TruePositives { get; init; }
    public double TrueNegatives { get; init; }
    public double FalsePositives { get; init; }
    public double FalseNegatives { get; init; }

    public double Accuracy => 
        (TruePositives + TrueNegatives) / (TruePositives + TrueNegatives + FalsePositives + FalseNegatives);
    
    public double Precision => 
        TruePositives / (TruePositives + FalsePositives);
    
    public double Recall => 
        TruePositives / (TruePositives + FalseNegatives);
    
    public double F1Score => 
        2 * (Precision * Recall) / (Precision + Recall);
}