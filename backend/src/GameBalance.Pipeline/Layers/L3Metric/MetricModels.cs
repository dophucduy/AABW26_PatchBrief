using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Layers.L3Metric;

public sealed class MetricResult
{
    /// <summary>First key: entity_id. Second key: bracket_id.</summary>
    [JsonPropertyName("metrics")]
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, CohortMetrics>> Metrics { get; init; }
}

public sealed class CohortMetrics
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("bracket_id")]
    public required string BracketId { get; init; }

    [JsonPropertyName("sources")]
    public required IReadOnlyDictionary<string, SourceMetrics> Sources { get; init; }

    [JsonPropertyName("comparison")]
    public SourceComparison? Comparison { get; init; }
}

public sealed class SourceMetrics
{
    [JsonPropertyName("event_count")]
    public int EventCount { get; init; }

    [JsonPropertyName("match_count")]
    public int MatchCount { get; init; }

    [JsonPropertyName("pick_count")]
    public int PickCount { get; init; }

    [JsonPropertyName("death_count")]
    public int DeathCount { get; init; }

    [JsonPropertyName("win_rate")]
    public double? WinRate { get; init; }

    [JsonPropertyName("pick_rate")]
    public double? PickRate { get; init; }

    [JsonPropertyName("death_rate")]
    public double? DeathRate { get; init; }
}

/// <summary>Offline/playtest value minus online/live value.</summary>
public sealed class SourceComparison
{
    [JsonPropertyName("offline_minus_online_win_rate")]
    public double? WinRateDelta { get; init; }

    [JsonPropertyName("offline_minus_online_pick_rate")]
    public double? PickRateDelta { get; init; }

    [JsonPropertyName("offline_minus_online_death_rate")]
    public double? DeathRateDelta { get; init; }
}
