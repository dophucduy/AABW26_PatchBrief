using System.Text.Json.Serialization;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;

namespace GameBalance.Api;

/// <summary>Response contract for the direct L0-L3 analysis workflow.</summary>
public sealed class AnalyzeResponse
{
    [JsonPropertyName("selected_source")]
    public required string SelectedSource { get; init; }

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
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("event_count")]
    public int EventCount { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
