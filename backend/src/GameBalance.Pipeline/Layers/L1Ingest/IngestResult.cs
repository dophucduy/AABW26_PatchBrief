using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Layers.L1Ingest;

/// <summary>
/// Describes a single event that was discarded during ingest, with enough
/// context (index, source, reason) to surface in a validation summary.
/// </summary>
public sealed record DiscardedEvent(int Index, string Source, string EventType, string Reason);

/// <summary>
/// Result of L1 ingest &amp; normalize: the accepted, source-tagged events plus a
/// validation summary of everything that was discarded and why (Requirement 4).
/// </summary>
public sealed class IngestResult
{
    /// <summary>Valid events, each tagged with a canonical <c>source</c> field.</summary>
    public IReadOnlyList<Dictionary<string, object?>> Events { get; init; } =
        Array.Empty<Dictionary<string, object?>>();

    /// <summary>Validated structured telemetry rows when the pipeline uses aggregate inputs.</summary>
    [JsonPropertyName("metric_rows")]
    public IReadOnlyList<Dictionary<string, object?>> MetricRows { get; init; } =
        Array.Empty<Dictionary<string, object?>>();

    /// <summary>Events dropped because they failed validation.</summary>
    public IReadOnlyList<DiscardedEvent> Discarded { get; init; } = Array.Empty<DiscardedEvent>();

    /// <summary>Human-readable warnings emitted while normalizing.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public int AcceptedCount => Events.Count + MetricRows.Count;

    public int DiscardedCount => Discarded.Count;

    /// <summary>Telemetry source tags preserved on accepted rows.</summary>
    public IReadOnlyList<string> Sources => Events
        .Select(item => item.TryGetValue("source", out object? value) ? value?.ToString() : null)
        .Concat(MetricRows.Select(item =>
            item.TryGetValue("source", out object? value) ? value?.ToString() : null))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToList();
}
