using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;

namespace GameBalance.Tests;

/// <summary>Verifies the API's direct L0 -> L1 -> L2/L3 call sequence.</summary>
public class PipelineWorkflowTests
{
    [Fact]
    public void Runs_raw_online_and_offline_events_through_l0_to_l3()
    {
        var online = new List<Dictionary<string, object?>>
        {
            RawEvent("Pick", "hero_A", 1, "online-1"),
            RawEvent("End", "hero_A", 2, "online-1", "win"),
        };
        var offline = new List<Dictionary<string, object?>>
        {
            RawEvent("Pick", "hero_A", 1, "offline-1"),
            RawEvent("End", "hero_A", 2, "offline-1", "loss"),
        };
        var adapter = new AdapterConfig
        {
            FieldMap = new()
            {
                ["kind"] = "event_type",
                ["hero"] = "entity_id",
                ["time"] = "timestamp",
            },
        };

        var adaptiveLayer = new AdaptiveLayer();
        var ingestLayer = new IngestNormalizeLayer();
        var semanticAnalyzer = new SemanticAnalyzer();
        var metricEngine = new MetricEngine();

        var (adaptedOnline, adaptedOffline) = adaptiveLayer.Apply(online, offline, adapter);
        IngestResult ingest = ingestLayer.Normalize(adaptedOnline.Events, adaptedOffline.Events);
        SemanticResult semantic = semanticAnalyzer.Analyze(ingest.Events, GameDefinition);
        MetricResult metric = metricEngine.Compute(ingest.Events);

        Assert.Equal(4, ingest.AcceptedCount);
        Assert.Contains(semantic.BracketEntities, item =>
            item.BracketId == "bronze" && item.EntityId == "hero_A");

        CohortMetrics cohort = metric.Metrics["hero_A"]["bronze"];
        Assert.Equal(1.0, cohort.Sources["online"].WinRate);
        Assert.Equal(0.0, cohort.Sources["offline"].WinRate);
        Assert.Equal(-1.0, cohort.Comparison!.WinRateDelta);
    }

    private static Dictionary<string, object?> RawEvent(
        string kind,
        string hero,
        int time,
        string matchId,
        string? result = null)
    {
        var value = new Dictionary<string, object?>
        {
            ["kind"] = kind switch
            {
                "Pick" => "entity_pick",
                "End" => "match_end",
                _ => kind,
            },
            ["hero"] = hero,
            ["time"] = time,
            ["bracket_id"] = "bronze",
            ["player_id"] = "player-1",
            ["match_id"] = matchId,
        };
        if (result is not null)
        {
            value["result"] = result;
        }

        return value;
    }

    private const string GameDefinition = """
        {
          "brackets": [
            { "id": "bronze", "label": "Low elo" },
            { "id": "diamond", "label": "High elo" }
          ]
        }
        """;
}
