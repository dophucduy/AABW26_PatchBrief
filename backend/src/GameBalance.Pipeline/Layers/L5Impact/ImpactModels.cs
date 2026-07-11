using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Layers.L5Impact;

public sealed class ImpactResult
{
    [JsonPropertyName("who_is_affected")]
    public required IReadOnlyList<AffectedCohort> WhoIsAffected { get; init; }

    [JsonPropertyName("alignment")]
    public required AlignmentAssessment Alignment { get; init; }
}

public sealed class AffectedCohort
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("entity_name")]
    public required string EntityName { get; init; }

    [JsonPropertyName("cohort")]
    public required string Cohort { get; init; }

    [JsonPropertyName("impact")]
    public required string Impact { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}

public sealed class AlignmentAssessment
{
    [JsonPropertyName("data_vs_community")]
    public required string DataVsCommunity { get; init; }

    [JsonPropertyName("playtest_vs_live")]
    public required string PlaytestVsLive { get; init; }

    [JsonPropertyName("patterns")]
    public required IReadOnlyList<ImpactPattern> Patterns { get; init; }
}

public sealed class ImpactPattern
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }

    [JsonPropertyName("evidence")]
    public required IReadOnlyList<string> Evidence { get; init; }
}
