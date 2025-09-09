using System.Diagnostics.Metrics;

namespace SRAAI.Shared.Services;

/// <summary>
/// Open telemetry activity source for the application.
/// </summary>
public class AppActivitySource
{
    public static readonly ActivitySource CurrentActivity = new("SRAAI", typeof(AppActivitySource).Assembly.GetName().Version!.ToString());

    public static readonly Meter CurrentMeter = new("SRAAI", typeof(AppActivitySource).Assembly.GetName().Version!.ToString());
}
