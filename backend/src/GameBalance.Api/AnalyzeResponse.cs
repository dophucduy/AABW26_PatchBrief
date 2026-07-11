using System.Text.Json.Serialization;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;
using GameBalance.Api.Community;

namespace GameBalance.Api;

/// <summary>Response contract for the full L0-L7 analysis workflow.</summary>
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

    [JsonPropertyName("l4_context")]
    public ContextBundle? Context { get; init; }

    [JsonPropertyName("community_source")]
    public CommunityScrapeResponse? CommunitySource { get; init; }

    [JsonPropertyName("l5_impact")]
    public ImpactResult? Impact { get; init; }

    [JsonPropertyName("l6_risk")]
    public RiskResult? Risk { get; init; }

    [JsonPropertyName("l7_report")]
    public InsightReport? Report { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class AdapterStageResponse
{
    [JsonPropertyName("live_record_count")]
    public int LiveRecordCount { get; init; }

    [JsonPropertyName("playtest_record_count")]
    public int PlaytestRecordCount { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
