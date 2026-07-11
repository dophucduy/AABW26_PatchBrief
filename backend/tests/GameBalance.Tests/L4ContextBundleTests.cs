using System.Text.Json;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L4Context;

namespace GameBalance.Tests;

public class L4ContextBundleTests
{
    private readonly ContextBundleBuilder _builder = new();

    [Fact]
    public void Builds_joined_changes_and_resolves_open_levers()
    {
        var metrics = new MetricResult
        {
            Metrics = new Dictionary<string, IReadOnlyDictionary<string, CohortMetrics>>
            {
                ["char_A"] = new Dictionary<string, CohortMetrics>(),
                ["unknown_metric_entity"] = new Dictionary<string, CohortMetrics>(),
            },
        };

        var result = _builder.Build(
            GameDefinitionJson,
            RulesJson,
            UpdatePlanJson,
            CommunityJson,
            metrics);

        var joined = Assert.Single(result.JoinedChanges);
        Assert.Equal("char_A", joined.EntityId);
        Assert.Equal("Ironclad", joined.EntityName);
        Assert.Equal("tank", joined.Role);
        Assert.Equal("base_damage", joined.Field);
        Assert.Equal("open", joined.LeverStatus);
        Assert.Contains(result.Warnings, warning =>
            warning.Contains("unknown_metric_entity", StringComparison.Ordinal));
    }

    [Fact]
    public void Rejects_update_plan_target_missing_from_game_definition()
    {
        const string invalidPlan = """
            {
              "version": "0.4.3",
              "changes": [
                { "target": "char_missing", "field": "damage", "from": 1, "to": 2 }
              ]
            }
            """;

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            _builder.Build(
                GameDefinitionJson,
                RulesJson,
                invalidPlan,
                CommunityJson));

        Assert.Contains("char_missing", exception.Message);
    }

    [Fact]
    public void Rejects_rules_entity_missing_from_game_definition()
    {
        const string invalidRules = """
            {
              "char_missing": { "locked": ["damage"], "open": [] }
            }
            """;

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            _builder.Build(
                GameDefinitionJson,
                invalidRules,
                UpdatePlanJson,
                CommunityJson));

        Assert.Contains("char_missing", exception.Message);
    }

    [Fact]
    public void Canonical_fixture_builds_the_expected_context_bundle()
    {
        string fixtureRoot = FindFixtureRoot();
        string Read(string name) => File.ReadAllText(Path.Combine(fixtureRoot, name));
        MetricResult metrics = JsonSerializer.Deserialize<MetricResult>(Read("metrics.json"))!;

        ContextBundle actual = _builder.Build(
            Read("game_definition.json"),
            Read("rules.json"),
            Read("update_plan.json"),
            Read("community.json"),
            metrics);
        ContextBundle expected = ContextBundleParser.Parse(Read("context_bundle.json"));

        Assert.Empty(actual.Warnings);
        Assert.Equal(expected.GameDefinition.GameId, actual.GameDefinition.GameId);
        Assert.Equal(expected.GameDefinition.Entities.Count, actual.GameDefinition.Entities.Count);
        Assert.Equal(expected.Community.Clusters.Count, actual.Community.Clusters.Count);
        Assert.Equal(expected.JoinedChanges.Count, actual.JoinedChanges.Count);
        Assert.Equal(
            expected.JoinedChanges.Select(change =>
                (change.EntityId, change.Field, change.LeverStatus)),
            actual.JoinedChanges.Select(change =>
                (change.EntityId, change.Field, change.LeverStatus)));
    }

    private const string GameDefinitionJson = """
        {
          "game_id": "arena_moba",
          "genre": "MOBA",
          "version": "0.4.2",
          "brackets": [
            { "id": "bronze", "label": "Low elo" },
            { "id": "diamond", "label": "High elo" }
          ],
          "entities": [
            {
              "id": "char_A",
              "name": "Ironclad",
              "type": "character",
              "role": "tank",
              "tags": ["frontline"],
              "stats": { "base_damage": 45 }
            }
          ]
        }
        """;

    private const string RulesJson = """
        {
          "char_A": {
            "locked": ["identity_skill_shield"],
            "open": ["base_damage"]
          }
        }
        """;

    private const string UpdatePlanJson = """
        {
          "version": "0.4.3",
          "changes": [
            {
              "target": "char_A",
              "field": "stats.base_damage",
              "from": 45,
              "to": 40,
              "delta": "-11%"
            }
          ]
        }
        """;

    private const string CommunityJson = """
        {
          "clusters": [
            {
              "entity_id": "char_A",
              "theme": "feels_weak",
              "volume": 12,
              "sentiment": "negative",
              "sample_quotes": ["Ironclad needs help"]
            }
          ]
        }
        """;

    private static string FindFixtureRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "fixtures", "l4_context_case");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("fixtures/l4_context_case was not found");
    }
}
