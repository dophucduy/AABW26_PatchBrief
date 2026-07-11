using System.Text.Json;
using GameBalance.Pipeline.Layers.L3Metric;

namespace GameBalance.Tests;

public class L3MetricTests
{
    private readonly MetricEngine _engine = new();

    [Fact]
    public void Computes_metrics_by_entity_bracket_and_source_with_comparison()
    {
        var events = new List<Dictionary<string, object?>>
        {
            Event("entity_pick", "char_A", "bronze", "online", "o1"),
            Event("death", "char_A", "bronze", "online", "o1"),
            Event("match_end", "char_A", "bronze", "online", "o1", "win"),
            Event("entity_pick", "char_A", "bronze", "online", "o2"),
            Event("match_end", "char_A", "bronze", "online", "o2", "loss"),

            Event("entity_pick", "char_A", "bronze", "offline", "f1"),
            Event("match_end", "char_A", "bronze", "offline", "f1", "win"),
        };

        CohortMetrics cohort = _engine.Compute(events).Metrics["char_A"]["bronze"];
        SourceMetrics online = cohort.Sources["online"];
        SourceMetrics offline = cohort.Sources["offline"];

        Assert.Equal(0.5, online.WinRate);
        Assert.Equal(1.0, online.PickRate);
        Assert.Equal(0.5, online.DeathRate);
        Assert.Equal(1.0, offline.WinRate);
        Assert.Equal(1.0, offline.PickRate);
        Assert.Equal(0.0, offline.DeathRate);

        Assert.NotNull(cohort.Comparison);
        Assert.Equal(0.5, cohort.Comparison.WinRateDelta);
        Assert.Equal(0.0, cohort.Comparison.PickRateDelta);
        Assert.Equal(-0.5, cohort.Comparison.DeathRateDelta);
    }

    [Fact]
    public void Uses_all_bracket_matches_as_pick_rate_denominator()
    {
        var events = new List<Dictionary<string, object?>>
        {
            Event("entity_pick", "char_A", "bronze", "online", "m1"),
            Event("match_end", "char_A", "bronze", "online", "m1", "win"),
            Event("entity_pick", "char_B", "bronze", "online", "m2"),
            Event("match_end", "char_B", "bronze", "online", "m2", "loss"),
        };

        MetricResult result = _engine.Compute(events);

        Assert.Equal(0.5, result.Metrics["char_A"]["bronze"].Sources["online"].PickRate);
        Assert.Equal(0.5, result.Metrics["char_B"]["bronze"].Sources["online"].PickRate);
    }

    [Fact]
    public void Reports_null_win_rate_when_no_match_end_exists()
    {
        var events = new List<Dictionary<string, object?>>
        {
            Event("entity_pick", "char_A", "diamond", "online", "m1"),
            Event("death", "char_A", "diamond", "online", "m1"),
        };

        SourceMetrics metrics = _engine.Compute(events).Metrics["char_A"]["diamond"].Sources["online"];

        Assert.Null(metrics.WinRate);
    }

    [Fact]
    public void Accepts_live_and_playtest_source_aliases_and_serializes_contract_names()
    {
        var events = new List<Dictionary<string, object?>>
        {
            Event("match_end", "char_A", "bronze", "live", "m1", "win"),
            Event("match_end", "char_A", "bronze", "playtest", "m2", "loss"),
        };

        MetricResult result = _engine.Compute(events);
        string json = JsonSerializer.Serialize(result);

        Assert.Contains("\"online\"", json);
        Assert.Contains("\"offline\"", json);
        Assert.Contains("\"win_rate\"", json);
        Assert.Contains("\"comparison\"", json);
    }

    private static Dictionary<string, object?> Event(
        string type,
        string entity,
        string bracket,
        string source,
        string match,
        string? result = null)
    {
        var value = new Dictionary<string, object?>
        {
            ["event_type"] = type,
            ["entity_id"] = entity,
            ["bracket_id"] = bracket,
            ["source"] = source,
            ["match_id"] = match,
        };
        if (result is not null) value["result"] = result;
        return value;
    }
}
