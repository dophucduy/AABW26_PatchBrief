using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Layers.L6Risk;

public sealed class RiskResult
{
    [JsonPropertyName("risks")]
    public required IReadOnlyList<RiskItem> Risks { get; init; }

    [JsonPropertyName("solution_paths")]
    public required IReadOnlyList<SolutionPath> SolutionPaths { get; init; }

    [JsonPropertyName("validation_plan")]
    public required IReadOnlyList<string> ValidationPlan { get; init; }
}

public sealed class RiskItem
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("evidence")]
    public required IReadOnlyList<string> Evidence { get; init; }
}

public sealed class SolutionPath
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }

    [JsonPropertyName("rationale")]
    public required string Rationale { get; init; }

    [JsonPropertyName("designer_decides")]
    public bool DesignerDecides { get; init; } = true;
}
