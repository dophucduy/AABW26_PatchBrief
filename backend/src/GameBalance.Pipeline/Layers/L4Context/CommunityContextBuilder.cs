using GameBalance.Pipeline.Contracts;

namespace GameBalance.Pipeline.Layers.L4Context;

/// <summary>
/// L4 community normalization. Maps feedback mentioning known entities to
/// those entity IDs and preserves unmatched feedback at game level.
/// </summary>
public sealed class CommunityContextBuilder
{
    private const int MaxSampleQuotes = 3;
    private const int MaxQuoteLength = 240;

    public ContextBundle Merge(
        ContextBundle context,
        IReadOnlyList<CommunityFeedbackItem> feedback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(feedback);

        IReadOnlyList<CommunityCluster> scrapedClusters = BuildClusters(
            context.GameDefinition,
            feedback);

        return new ContextBundle
        {
            GameDefinition = context.GameDefinition,
            Rules = context.Rules,
            UpdatePlan = context.UpdatePlan,
            Community = new CommunitySnapshot
            {
                Clusters = context.Community.Clusters.Concat(scrapedClusters).ToList(),
            },
            JoinedChanges = context.JoinedChanges,
            Warnings = context.Warnings,
        };
    }

    public IReadOnlyList<CommunityCluster> BuildClusters(
        GameDefinitionSnapshot gameDefinition,
        IReadOnlyList<CommunityFeedbackItem> feedback)
    {
        string gameLevelId = string.IsNullOrWhiteSpace(gameDefinition.GameId)
            ? "game"
            : $"game:{gameDefinition.GameId}";

        var tagged = new List<TaggedFeedback>();
        foreach (CommunityFeedbackItem item in feedback.Where(item =>
                     !string.IsNullOrWhiteSpace(item.Text)))
        {
            List<GameEntity> matches = gameDefinition.Entities
                .Where(entity => Mentions(item.Text, entity))
                .ToList();

            if (matches.Count == 0)
            {
                tagged.Add(new TaggedFeedback(gameLevelId, NormalizeSentiment(item), item));
                continue;
            }

            tagged.AddRange(matches.Select(entity =>
                new TaggedFeedback(entity.Id, NormalizeSentiment(item), item)));
        }

        return tagged
            .GroupBy(item => (item.EntityId, item.Sentiment))
            .Select(group => new CommunityCluster
            {
                EntityId = group.Key.EntityId,
                Theme = $"steam_{group.Key.Sentiment}_feedback",
                Volume = group.Count(),
                Sentiment = group.Key.Sentiment,
                SampleQuotes = group
                    .OrderByDescending(item => item.Feedback.HelpfulVotes)
                    .ThenByDescending(item => item.Feedback.CreatedAt)
                    .Select(item => Truncate(item.Feedback.Text))
                    .Distinct(StringComparer.Ordinal)
                    .Take(MaxSampleQuotes)
                    .ToList(),
            })
            .OrderBy(cluster => cluster.EntityId, StringComparer.Ordinal)
            .ThenBy(cluster => cluster.Sentiment, StringComparer.Ordinal)
            .ToList();
    }

    private static bool Mentions(string text, GameEntity entity) =>
        ContainsTerm(text, entity.Name) || ContainsTerm(text, entity.Id);

    private static bool ContainsTerm(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return false;
        }

        return text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSentiment(CommunityFeedbackItem item) =>
        item.Sentiment?.ToLowerInvariant() switch
        {
            "positive" or "negative" => item.Sentiment.ToLowerInvariant(),
            _ => "neutral",
        };

    private static string Truncate(string value) => value.Length <= MaxQuoteLength
        ? value
        : value[..MaxQuoteLength] + "…";

    private sealed record TaggedFeedback(
        string EntityId,
        string Sentiment,
        CommunityFeedbackItem Feedback);
}
