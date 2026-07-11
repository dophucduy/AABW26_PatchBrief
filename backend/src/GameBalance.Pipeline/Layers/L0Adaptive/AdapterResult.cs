namespace GameBalance.Pipeline.Layers.L0Adaptive;

/// <summary>
/// Result of applying the adapter to an event stream: the adapted events
/// plus any non-fatal warnings (e.g. mappings for fields that never appeared).
/// </summary>
public sealed class AdapterResult
{
    public required IReadOnlyList<Dictionary<string, object?>> Events { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
