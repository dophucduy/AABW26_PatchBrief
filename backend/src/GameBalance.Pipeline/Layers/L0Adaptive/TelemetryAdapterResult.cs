namespace GameBalance.Pipeline.Layers.L0Adaptive;

/// <summary>
/// Result of applying the adapter to a structured telemetry export.
/// </summary>
public sealed class TelemetryAdapterResult
{
    public required IReadOnlyList<Dictionary<string, object?>> Records { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
