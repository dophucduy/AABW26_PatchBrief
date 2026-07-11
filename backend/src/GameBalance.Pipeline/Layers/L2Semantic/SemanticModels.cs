using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Layers.L2Semantic;

public sealed class SemanticResult
{
    [JsonPropertyName("bracket_profiles")]
    public required IReadOnlyList<BracketBehaviorProfile> BracketProfiles { get; init; }

    [JsonPropertyName("bracket_entities")]
    public required IReadOnlyList<BracketEntitySummary> BracketEntities { get; init; }

    [JsonPropertyName("patterns")]
    public required IReadOnlyList<BehaviorPattern> Patterns { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class BracketBehaviorProfile
{
    [JsonPropertyName("bracket_id")]
    public required string BracketId { get; init; }

    [JsonPropertyName("event_count")]
    public int EventCount { get; init; }

    [JsonPropertyName("player_count")]
    public int PlayerCount { get; init; }

    [JsonPropertyName("play_patterns")]
    public required IReadOnlyDictionary<string, int> PlayPatterns { get; init; }

    [JsonPropertyName("entity_preferences")]
    public required IReadOnlyList<EntityPreference> EntityPreferences { get; init; }
}

public sealed record EntityPreference(
    [property: JsonPropertyName("entity_id")] string EntityId,
    [property: JsonPropertyName("pick_count")] int PickCount,
    [property: JsonPropertyName("pick_share")] double PickShare);

public sealed class BracketEntitySummary
{
    [JsonPropertyName("bracket_id")]
    public required string BracketId { get; init; }

    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("event_count")]
    public int EventCount { get; init; }

    [JsonPropertyName("pick_share")]
    public double PickShare { get; init; }

    [JsonPropertyName("win_rate")]
    public double? WinRate { get; init; }

    [JsonPropertyName("low_confidence")]
    public bool LowConfidence { get; init; }
}

public sealed class BehaviorPattern
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("bracket_id")]
    public string? BracketId { get; init; }

    [JsonPropertyName("entity_id")]
    public string? EntityId { get; init; }

    [JsonPropertyName("player_id")]
    public string? PlayerId { get; init; }

    [JsonPropertyName("details")]
    public required string Details { get; init; }

    [JsonPropertyName("evidence")]
    public required IReadOnlyList<string> Evidence { get; init; }

    [JsonPropertyName("low_confidence")]
    public bool LowConfidence { get; init; }
}
