using Microsoft.ML.Transforms;

namespace Analiz.Application.Transform;

public class TimeFeatureTransform : CustomMappingFactory<TimeFeatureInput,
    TimeFeatureOutput>
{
    public override Action<TimeFeatureInput, TimeFeatureOutput> GetMapping()
    {
        return (input, output) =>
        {
            const double daySeconds = 24 * 60 * 60;
            output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / daySeconds);
            output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / daySeconds);
            output.LogAmount = (float)Math.Log(input.Amount + 1);
        };
    }
}

public class TimeFeatureInput
{
    public float Time { get; set; }
    public float Amount { get; set; }
}

public class TimeFeatureOutput
{
    public float TimeSin { get; set; }
    public float TimeCos { get; set; }
    public float LogAmount { get; set; }
}