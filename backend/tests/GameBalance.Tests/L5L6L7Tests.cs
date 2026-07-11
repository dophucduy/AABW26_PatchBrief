using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;

namespace GameBalance.Tests;

public class L5ImpactTests
{
    private readonly ImpactAnalyzer _analyzer = new();

    [Fact]
    public void Detects_bracket_split_and_perception_divergence_for_demo_case()
    {
        MetricResult metrics = DemoFixtures.BuildMetrics();
        ContextBundle context = DemoFixtures.BuildContext();

        ImpactResult result = _analyzer.Analyze(metrics, context);

        Assert.Contains(result.Alignment.Patterns, pattern =>
            pattern.Id == "bracket_split_easy_low" && pattern.EntityId == "char_A");
        Assert.Contains(result.Alignment.Patterns, pattern =>
            pattern.Id == "perception_vs_data_divergence" && pattern.EntityId == "char_A");
        Assert.Equal("divergent", result.Alignment.DataVsCommunity);
        Assert.NotEmpty(result.WhoIsAffected);
    }

    [Fact]
    public void Detects_plan_conflict_when_nerfing_strong_low_bracket_entity()
    {
        MetricResult metrics = DemoFixtures.BuildMetrics();
        ContextBundle context = DemoFixtures.BuildContext();

        ImpactResult result = _analyzer.Analyze(metrics, context);

        Assert.Contains(result.Alignment.Patterns, pattern =>
            pattern.Id == "plan_conflicts_with_data" && pattern.EntityId == "char_A");
    }
}

public class L6RiskTests
{
    private readonly ImpactAnalyzer _impactAnalyzer = new();
    private readonly RiskFramer _riskFramer = new();

    [Fact]
    public void Maps_perception_divergence_to_stakeholder_conflict_and_comms_path()
    {
        MetricResult metrics = DemoFixtures.BuildMetrics();
        ContextBundle context = DemoFixtures.BuildContext();
        ImpactResult impact = _impactAnalyzer.Analyze(metrics, context);

        RiskResult result = _riskFramer.Analyze(impact, context);

        Assert.Contains(result.Risks, risk => risk.Id == "stakeholder_conflict");
        Assert.Contains(result.SolutionPaths, path => path.Type == "comms_only");
        Assert.All(result.SolutionPaths, path => Assert.True(path.DesignerDecides));
        Assert.NotEmpty(result.ValidationPlan);
    }
}

public class L7ReportTests
{
    private readonly ImpactAnalyzer _impactAnalyzer = new();
    private readonly RiskFramer _riskFramer = new();
    private readonly ReportGenerator _reportGenerator = new();

    [Fact]
    public async Task Falls_back_to_template_report_without_api_key()
    {
        MetricResult metrics = DemoFixtures.BuildMetrics();
        ContextBundle context = DemoFixtures.BuildContext();
        ImpactResult impact = _impactAnalyzer.Analyze(metrics, context);
        RiskResult risks = _riskFramer.Analyze(impact, context);

        InsightReport report = await _reportGenerator.GenerateAsync(
            metrics,
            context,
            impact,
            risks,
            options: null,
            httpClient: null);

        Assert.Equal("template", report.GeneratedBy);
        Assert.False(string.IsNullOrWhiteSpace(report.ExecutiveSummary));
        Assert.Contains("Patch Brief", report.ReportMarkdown);
        Assert.False(string.IsNullOrWhiteSpace(report.DraftPlayerComms));
        Assert.Equal(impact.WhoIsAffected.Count, report.WhoIsAffected.Count);
    }
}

internal static class DemoFixtures
{
    public static MetricResult BuildMetrics()
    {
        var engine = new MetricEngine();
        return engine.Compute(BuildTelemetryEvents());
    }

    public static ContextBundle BuildContext() =>
        ContextBundleParser.Parse(ContextBundleJson);

    private const string ContextBundleJson = """
        {
          "game_definition": {
            "game_id": "arena_moba",
            "brackets": [
              { "id": "bronze", "label": "Low elo" },
              { "id": "diamond", "label": "High elo" }
            ],
            "entities": [
              {
                "id": "char_A",
                "name": "Ironclad",
                "role": "tank",
                "stats": { "base_damage": 45 }
              }
            ]
          },
          "rules": {
            "char_A": {
              "locked": ["identity_skill_shield"],
              "open": ["base_damage"]
            }
          },
          "update_plan": {
            "version": "0.4.3",
            "changes": [
              { "target": "char_A", "field": "base_damage", "from": 45, "to": 40, "delta": "-11%" }
            ]
          },
          "community": {
            "clusters": [
              { "entity_id": "char_A", "theme": "feels_weak", "volume": 340, "sentiment": "negative" }
            ]
          },
          "joined_changes": [
            {
              "entity_id": "char_A",
              "entity_name": "Ironclad",
              "role": "tank",
              "field": "base_damage",
              "from": 45,
              "to": 40,
              "delta": "-11%",
              "lever_status": "open"
            }
          ],
          "warnings": []
        }
        """;

    private static List<Dictionary<string, object?>> BuildTelemetryEvents()
    {
        var events = new List<Dictionary<string, object?>>();
        int matchId = 1;

        for (int i = 0; i < 12; i++)
        {
            string match = $"bronze-online-{matchId}";
            events.Add(Event("entity_pick", "char_A", "bronze", "online", match));
            events.Add(Event("match_end", "char_A", "bronze", "online", match, i < 7 ? "win" : "loss"));
            matchId++;
        }

        for (int i = 0; i < 38; i++)
        {
            string match = $"bronze-online-{matchId}";
            events.Add(Event("entity_pick", "char_B", "bronze", "online", match));
            events.Add(Event("match_end", "char_B", "bronze", "online", match, "win"));
            matchId++;
        }

        for (int i = 0; i < 100; i++)
        {
            string match = $"diamond-online-{matchId}";
            events.Add(Event("entity_pick", "char_A", "diamond", "online", match));
            events.Add(Event("match_end", "char_A", "diamond", "online", match, i < 49 ? "win" : "loss"));
            matchId++;
        }

        for (int i = 0; i < 20; i++)
        {
            string match = $"bronze-offline-{matchId}";
            events.Add(Event("entity_pick", "char_A", "bronze", "offline", match));
            events.Add(Event("match_end", "char_A", "bronze", "offline", match, i < 10 ? "win" : "loss"));
            matchId++;
        }

        return events;
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
        if (result is not null)
        {
            value["result"] = result;
        }

        return value;
    }
}
