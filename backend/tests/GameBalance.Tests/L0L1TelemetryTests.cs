using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;

namespace GameBalance.Tests;

public class L0TelemetryAdapterTests
{
    private readonly AdaptiveLayer _layer = new();

    [Fact]
    public void Maps_studio_metric_fields_to_canonical()
    {
        const string json = """
            {
              "telemetry": [
                { "hero": "char_A", "bracket": "bronze", "wr": 0.58, "pr": 0.22 }
              ]
            }
            """;
        var adapter = new AdapterConfig
        {
            MetricMap = new()
            {
                ["hero"] = "entity_id",
                ["bracket"] = "bracket_id",
                ["wr"] = "win_rate",
                ["pr"] = "pick_rate",
            },
        };

        TelemetryAdapterResult result = _layer.AdaptTelemetry(json, adapter);

        var row = result.Records[0];
        Assert.Equal("char_A", row["entity_id"]);
        Assert.Equal("bronze", row["bracket_id"]);
        Assert.Equal(0.58, row["win_rate"]);
        Assert.Equal(0.22, row["pick_rate"]);
    }

    [Fact]
    public void ApplyTelemetry_handles_live_and_playtest_exports()
    {
        const string live = """{ "telemetry": [{ "entity_id": "char_A", "bracket_id": "bronze", "win_rate": 0.58 }] }""";
        const string playtest = """{ "telemetry": [{ "entity_id": "char_A", "bracket_id": "bronze", "win_rate": 0.50 }] }""";

        var (liveResult, playtestResult) = _layer.ApplyTelemetry(live, playtest, null);

        Assert.Single(liveResult.Records);
        Assert.Single(playtestResult.Records);
    }
}

public class L1TelemetryIngestTests
{
    private readonly TelemetryIngestLayer _layer = new();

    [Fact]
    public void Tags_live_rows_with_online_source()
    {
        var live = new List<Dictionary<string, object?>>
        {
            Row("char_A", "bronze", winRate: 0.58, pickRate: 0.22),
        };

        TelemetryIngestResult result = _layer.Normalize(live, []);

        Assert.Equal("online", result.Records[0]["source"]);
    }

    [Fact]
    public void Tags_playtest_rows_with_offline_source()
    {
        var playtest = new List<Dictionary<string, object?>>
        {
            Row("char_A", "bronze", winRate: 0.50),
        };

        TelemetryIngestResult result = _layer.Normalize([], playtest);

        Assert.Equal("offline", result.Records[0]["source"]);
    }

    [Fact]
    public void Discards_rows_missing_required_fields()
    {
        var live = new List<Dictionary<string, object?>>
        {
            new() { ["entity_id"] = "char_A", ["win_rate"] = 0.58 },
        };

        TelemetryIngestResult result = _layer.Normalize(live, []);

        Assert.Empty(result.Records);
        Assert.Single(result.Discarded);
    }

    [Fact]
    public void Rejects_rates_outside_zero_to_one()
    {
        var live = new List<Dictionary<string, object?>>
        {
            Row("char_A", "bronze", winRate: 1.2),
        };

        TelemetryIngestResult result = _layer.Normalize(live, []);

        Assert.Empty(result.Records);
    }

    private static Dictionary<string, object?> Row(
        string entityId,
        string bracketId,
        double? winRate = null,
        double? pickRate = null)
    {
        var row = new Dictionary<string, object?>
        {
            ["entity_id"] = entityId,
            ["bracket_id"] = bracketId,
            ["sessions"] = 100,
        };
        if (winRate.HasValue) row["win_rate"] = winRate.Value;
        if (pickRate.HasValue) row["pick_rate"] = pickRate.Value;
        return row;
    }
}
