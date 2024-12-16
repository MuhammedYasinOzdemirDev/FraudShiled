using System.Text.Json;
using System.Text.Json.Serialization;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.ValueObjects;

namespace FraudShield.TransactionAnalysis.Infrastructure.Converters;

public class RiskFactorJsonConverter : JsonConverter<RiskFactor>
{
    public override RiskFactor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;

            var code = root.GetProperty("code").GetString();
            var description = root.GetProperty("description").GetString();
            var impact = root.GetProperty("impact").GetDecimal();
            var category = Enum.Parse<RiskFactorCategory>(root.GetProperty("category").GetString());
            
            var evidence = new Dictionary<string, string>();
            if (root.TryGetProperty("evidence", out JsonElement evidenceElement))
            {
                foreach (var item in evidenceElement.EnumerateObject())
                {
                    evidence.Add(item.Name, item.Value.GetString());
                }
            }

            return RiskFactor.Create(code, description, impact, category, evidence);
        }
    }

    public override void Write(Utf8JsonWriter writer, RiskFactor value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString("code", value.Code);
        writer.WriteString("description", value.Description);
        writer.WriteNumber("impact", value.Impact);
        writer.WriteString("category", value.Category.ToString());
        writer.WriteString("detectedAt", value.DetectedAt.ToString("O"));
        
        writer.WriteStartObject("evidence");
        foreach (var item in value.Evidence)
        {
            writer.WriteString(item.Key, item.Value);
        }
        writer.WriteEndObject();
        
        writer.WriteEndObject();
    }
}
