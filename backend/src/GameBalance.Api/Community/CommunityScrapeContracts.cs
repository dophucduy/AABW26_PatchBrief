using System.Text.Json.Serialization;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L4Context;

namespace GameBalance.Api.Community;

public sealed class CommunityScrapeRequest
{
    [JsonPropertyName("game_name")]
    public required string GameName { get; init; }

    [JsonPropertyName("max_reviews")]
    public int MaxReviews { get; init; } = 100;

    [JsonPropertyName("language")]
    public string Language { get; init; } = "english";

    [JsonPropertyName("review_type")]
    public string ReviewType { get; init; } = "all";
}

public sealed class ApifyCommunityResult
{
    public required string RunId { get; init; }

    public required string DatasetId { get; init; }

    public required string GameName { get; init; }

    public required IReadOnlyList<CommunityFeedbackItem> Feedback { get; init; }
}

public sealed class CommunityScrapeResponse
{
    [JsonPropertyName("source")]
    public string Source { get; init; } = "steam";

    [JsonPropertyName("game_name")]
    public required string GameName { get; init; }

    [JsonPropertyName("run_id")]
    public required string RunId { get; init; }

    [JsonPropertyName("dataset_id")]
    public required string DatasetId { get; init; }

    [JsonPropertyName("review_count")]
    public int ReviewCount { get; init; }

    [JsonPropertyName("community")]
    public required CommunitySnapshot Community { get; init; }
}
