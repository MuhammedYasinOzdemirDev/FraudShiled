namespace FraudShield.TransactionAnalysis.Domain.Enums;
public enum FeatureType
{
    Numeric,
    Categorical,
    Boolean,
    DateTime,
    Text
}

public enum FeatureTransformationType
{
    Temporal,
    Numeric,
    Categorical,
    Text,
    Interaction
}