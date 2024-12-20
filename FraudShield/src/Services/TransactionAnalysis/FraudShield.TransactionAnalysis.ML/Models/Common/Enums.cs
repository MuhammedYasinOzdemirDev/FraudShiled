namespace FraudShield.TransactionAnalysis.ML.Models.Common.Enums;

public enum ModelType
{
    LightGBM,
    PCA,
    Other
}

public enum ModelStatus
{
    Draft,
    Training,
    Active,
    Failed,
    Archived
}
