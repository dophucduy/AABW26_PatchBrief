using System.Text.Json.Serialization;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;

namespace GameBalance.Api;

/// <summary>Response contract for the direct L0-L3 analysis workflow.</summary>
public sealed class AnalyzeResponse
{
    [JsonPropertyName("l0_adapter")]
    public required AdapterStageResponse Adapter { get; init; }

    [JsonPropertyName("l1_ingest")]
    public required IngestResult Ingest { get; init; }

    [JsonPropertyName("l2_semantic")]
    public required SemanticResult Semantic { get; init; }

    [JsonPropertyName("l3_metric")]
    public required MetricResult Metric { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class AdapterStageResponse
{
    [JsonPropertyName("online_event_count")]
    public int OnlineEventCount { get; init; }

    [JsonPropertyName("offline_event_count")]
    public int OfflineEventCount { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
