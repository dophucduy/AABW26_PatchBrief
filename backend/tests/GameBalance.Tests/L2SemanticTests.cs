using GameBalance.Pipeline.Layers.L2Semantic;

namespace GameBalance.Tests;

public class L2SemanticTests
{
    private static readonly BracketDefinition[] Brackets =
    {
        new("bronze", "Low", 0, 999),
        new("diamond", "High", 1000, 2000),
    };

    private readonly SemanticAnalyzer _analyzer = new();

    [Fact]
    public void Segments_by_declared_bracket_and_rating_and_builds_profiles()
    {
        var events = new List<Dictionary<string, object?>>
        {
            Event("entity_pick", "char_A", bracket: "bronze", player: "p1", match: "b1"),
            Event("ability_used", "char_A", rating: 1500, player: "p2", match: "d1"),
        };

        SemanticResult result = _analyzer.Analyze(events, Brackets);

        BracketBehaviorProfile bronze = Assert.Single(result.BracketProfiles, x => x.BracketId == "bronze");
        BracketBehaviorProfile diamond = Assert.Single(result.BracketProfiles, x => x.BracketId == "diamond");
        Assert.Equal(1, bronze.PlayPatterns["entity_pick"]);
        Assert.Equal(1, diamond.PlayPatterns["ability_used"]);
        Assert.Equal("char_A", Assert.Single(bronze.EntityPreferences).EntityId);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Flags_one_trick_meta_dominant_and_bracket_split()
    {
        var events = new List<Dictionary<string, object?>>();
        for (int i = 1; i <= 4; i++)
        {
            events.Add(Event("entity_pick", "char_A", "bronze", "p1", $"b{i}"));
            events.Add(Event("match_end", "char_A", "bronze", "p1", $"b{i}", result: "win"));
        }

        events.Add(Event("entity_pick", "char_A", "diamond", "p2", "d1"));
        events.Add(Event("match_end", "char_A", "diamond", "p2", "d1", result: "loss"));
        for (int i = 2; i <= 4; i++)
        {
            events.Add(Event("entity_pick", "char_B", "diamond", "p2", $"d{i}"));
            events.Add(Event("match_end", "char_B", "diamond", "p2", $"d{i}", result: "win"));
        }

        SemanticResult result = _analyzer.Analyze(events, Brackets);

        BehaviorPattern oneTrick = Assert.Single(result.Patterns, x => x.Type == "one_trick");
        Assert.Equal("p1", oneTrick.PlayerId);
        Assert.Equal("char_A", oneTrick.EntityId);

        Assert.Contains(result.Patterns, x =>
            x.Type == "meta_dominant" && x.BracketId == "bronze" && x.EntityId == "char_A");
        Assert.Contains(result.Patterns, x =>
            x.Type == "meta_dominant" && x.BracketId == "diamond" && x.EntityId == "char_B");

        BehaviorPattern split = Assert.Single(result.Patterns, x =>
            x.Type == "bracket_split" && x.EntityId == "char_A");
        Assert.True(split.LowConfidence);
    }

    [Fact]
    public void Marks_bracket_entity_with_fewer_than_five_events_low_confidence()
    {
        var events = new List<Dictionary<string, object?>>
        {
            Event("entity_pick", "char_A", bracket: "bronze", match: "b1"),
            Event("death", "char_A", bracket: "bronze", match: "b1"),
            Event("match_end", "char_A", bracket: "bronze", match: "b1", result: "loss"),
            Event("ability_used", "char_A", bracket: "bronze", match: "b1"),
        };

        BracketEntitySummary summary = Assert.Single(_analyzer.Analyze(events, Brackets).BracketEntities);

        Assert.Equal(4, summary.EventCount);
        Assert.True(summary.LowConfidence);
    }

    [Fact]
    public void Reads_brackets_from_game_definition_json()
    {
        const string json = """
            {
              "brackets": [
                { "id": "bronze", "label": "Low", "min_rating": 0, "max_rating": 999 },
                { "id": "diamond", "label": "High", "min_rating": 1000, "max_rating": 2000 }
              ]
            }
            """;
        var events = new List<Dictionary<string, object?>>
        {
            Event("entity_pick", "char_A", rating: 1200, match: "d1"),
        };

        SemanticResult result = _analyzer.Analyze(events, json);

        Assert.Contains(result.BracketEntities, x => x.BracketId == "diamond");
    }

    private static Dictionary<string, object?> Event(
        string type,
        string entity,
        string? bracket = null,
        string? player = null,
        string? match = null,
        string? result = null,
        double? rating = null)
    {
        var value = new Dictionary<string, object?>
        {
            ["event_type"] = type,
            ["entity_id"] = entity,
        };
        if (bracket is not null) value["bracket_id"] = bracket;
        if (player is not null) value["player_id"] = player;
        if (match is not null) value["match_id"] = match;
        if (result is not null) value["result"] = result;
        if (rating.HasValue) value["skill_rating"] = rating.Value;
        return value;
    }
}
