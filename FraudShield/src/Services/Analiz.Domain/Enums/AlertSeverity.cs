namespace FraudShield.TransactionAnalysis.Domain.Enums;

/// <summary>
/// Dolandırıcılık uyarısının önem derecesini tanımlar
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Düşük öncelikli uyarı - İnceleme gerektirir ancak acil değil
    /// </summary>
    Low = 0,

    /// <summary>
    /// Orta öncelikli uyarı - Yakın zamanda inceleme gerektirir
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Yüksek öncelikli uyarı - Acil inceleme gerektirir
    /// </summary>
    High = 2,

    /// <summary>
    /// Kritik uyarı - Anında müdahale ve inceleme gerektirir
    /// </summary>
    Critical = 3
}
