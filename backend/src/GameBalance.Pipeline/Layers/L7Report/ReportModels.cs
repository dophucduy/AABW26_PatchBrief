using System.Text.Json.Serialization;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;

namespace GameBalance.Pipeline.Layers.L7Report;

public sealed class InsightReport
{
    [JsonPropertyName("executive_summary")]
    public required string ExecutiveSummary { get; init; }

    [JsonPropertyName("who_is_affected")]
    public required IReadOnlyList<AffectedCohort> WhoIsAffected { get; init; }

    [JsonPropertyName("alignment")]
    public required AlignmentAssessment Alignment { get; init; }

    [JsonPropertyName("risks")]
    public required IReadOnlyList<RiskItem> Risks { get; init; }

    [JsonPropertyName("solution_paths")]
    public required IReadOnlyList<SolutionPath> SolutionPaths { get; init; }

    [JsonPropertyName("validation_plan")]
    public required IReadOnlyList<string> ValidationPlan { get; init; }

    [JsonPropertyName("report_markdown")]
    public required string ReportMarkdown { get; init; }

    [JsonPropertyName("draft_player_comms")]
    public string? DraftPlayerComms { get; init; }

    [JsonPropertyName("generated_by")]
    public required string GeneratedBy { get; init; }
}

public sealed class LlmOptions
{
    public string Provider { get; init; } = "OpenAI";
    public string? ApiKey { get; init; }
    public string Model { get; init; } = "gpt-4o-mini";
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public int TimeoutSeconds { get; init; } = 30;
}
