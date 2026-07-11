using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;

namespace GameBalance.Tests;

/// <summary>Verifies the API's structured telemetry path through L0 -> L1 -> L2/L3.</summary>
public class PipelineWorkflowTests
{
    [Fact]
    public void Runs_structured_telemetry_through_l0_to_l3()
    {
        const string live = """
            {
              "telemetry": [
                { "entity_id": "char_A", "bracket_id": "bronze", "sessions": 100, "win_rate": 1.0, "pick_rate": 1.0 },
                { "entity_id": "char_B", "bracket_id": "bronze", "sessions": 100, "win_rate": 0.0, "pick_rate": 0.0 }
              ]
            }
            """;
        const string playtest = """
            {
              "telemetry": [
                { "entity_id": "char_A", "bracket_id": "bronze", "sessions": 100, "win_rate": 0.0, "pick_rate": 1.0 }
              ]
            }
            """;

        var adaptiveLayer = new AdaptiveLayer();
        var telemetryIngestLayer = new TelemetryIngestLayer();
        var semanticAnalyzer = new SemanticAnalyzer();
        var metricEngine = new MetricEngine();

        var (adaptedLive, adaptedPlaytest) = adaptiveLayer.ApplyTelemetry(live, playtest, null);
        TelemetryIngestResult ingest = telemetryIngestLayer.Normalize(
            adaptedLive.Records,
            adaptedPlaytest.Records);
        MetricResult metric = metricEngine.FromTelemetry(ingest.Records);
        SemanticResult semantic = semanticAnalyzer.AnalyzeFromMetrics(metric, GameDefinition);

        Assert.Equal(3, ingest.AcceptedCount);
        Assert.Contains(semantic.BracketEntities, item =>
            item.BracketId == "bronze" && item.EntityId == "char_A");

        CohortMetrics cohort = metric.Metrics["char_A"]["bronze"];
        Assert.Equal(1.0, cohort.Sources["online"].WinRate);
        Assert.Equal(0.0, cohort.Sources["offline"].WinRate);
        Assert.Equal(-1.0, cohort.Comparison!.WinRateDelta);
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
