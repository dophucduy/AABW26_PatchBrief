namespace GameBalance.Pipeline.Layers.L4Context;

/// <summary>Provider-neutral community feedback consumed by L4.</summary>
public sealed record CommunityFeedbackItem(
    string Source,
    string GameName,
    string Text,
    string? Sentiment,
    string? Language,
    DateTimeOffset? CreatedAt,
    int HelpfulVotes = 0);
