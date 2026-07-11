using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Contracts;

/// <summary>Shared contract produced by L4 and consumed by L5-L7.</summary>
public sealed class ContextBundle
{
    [JsonPropertyName("game_definition")]
    public required GameDefinitionSnapshot GameDefinition { get; init; }

    [JsonPropertyName("rules")]
    public required IReadOnlyDictionary<string, EntityRules> Rules { get; init; }

    [JsonPropertyName("update_plan")]
    public required UpdatePlanSnapshot UpdatePlan { get; init; }

    [JsonPropertyName("community")]
    public required CommunitySnapshot Community { get; init; }

    [JsonPropertyName("joined_changes")]
    public required IReadOnlyList<JoinedChange> JoinedChanges { get; init; }

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class GameDefinitionSnapshot
{
    [JsonPropertyName("game_id")]
    public string? GameId { get; init; }

    [JsonPropertyName("genre")]
    public string? Genre { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("brackets")]
    public IReadOnlyList<BracketSnapshot> Brackets { get; init; } = Array.Empty<BracketSnapshot>();

    [JsonPropertyName("entities")]
    public required IReadOnlyList<GameEntity> Entities { get; init; }
}

public sealed class BracketSnapshot
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("label")]
    public string? Label { get; init; }
}

public sealed class GameEntity
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    [JsonPropertyName("stats")]
    public IReadOnlyDictionary<string, object?> Stats { get; init; } =
        new Dictionary<string, object?>();

    [JsonPropertyName("intentional_difficulty")]
    public string? IntentionalDifficulty { get; init; }
}

public sealed class EntityRules
{
    [JsonPropertyName("locked")]
    public IReadOnlyList<string> Locked { get; init; } = Array.Empty<string>();

    [JsonPropertyName("open")]
    public IReadOnlyList<string> Open { get; init; } = Array.Empty<string>();
}

public sealed class UpdatePlanSnapshot
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("changes")]
    public required IReadOnlyList<PlannedChange> Changes { get; init; }
}

public sealed class PlannedChange
{
    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("from")]
    public object? From { get; init; }

    [JsonPropertyName("to")]
    public object? To { get; init; }

    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}

public sealed class CommunitySnapshot
{
    [JsonPropertyName("clusters")]
    public required IReadOnlyList<CommunityCluster> Clusters { get; init; }
}

public sealed class CommunityCluster
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("theme")]
    public required string Theme { get; init; }

    [JsonPropertyName("volume")]
    public int Volume { get; init; }

    [JsonPropertyName("sentiment")]
    public string? Sentiment { get; init; }

    [JsonPropertyName("sample_quotes")]
    public IReadOnlyList<string> SampleQuotes { get; init; } = Array.Empty<string>();
}

public sealed class JoinedChange
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("entity_name")]
    public required string EntityName { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("from")]
    public object? From { get; init; }

    [JsonPropertyName("to")]
    public object? To { get; init; }

    [JsonPropertyName("delta")]
    public string? Delta { get; init; }

    [JsonPropertyName("lever_status")]
    public required string LeverStatus { get; init; }
}
