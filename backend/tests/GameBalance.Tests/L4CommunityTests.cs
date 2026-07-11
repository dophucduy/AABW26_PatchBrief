using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L4Context;

namespace GameBalance.Tests;

public class L4CommunityTests
{
    private readonly CommunityContextBuilder _builder = new();

    [Fact]
    public void Maps_reviews_mentioning_entity_names_to_entity_clusters()
    {
        GameDefinitionSnapshot game = GameDefinition();
        var feedback = new List<CommunityFeedbackItem>
        {
            new("steam", "Strinova", "Audrey feels weak after the patch", "negative", "english", null),
            new("steam", "Strinova", "Audrey is fun to play", "positive", "english", null),
            new("steam", "Strinova", "The servers are unstable", "negative", "english", null),
        };

        IReadOnlyList<CommunityCluster> clusters = _builder.BuildClusters(game, feedback);

        Assert.Contains(clusters, cluster =>
            cluster.EntityId == "char_audrey" &&
            cluster.Sentiment == "negative" &&
            cluster.Volume == 1);
        Assert.Contains(clusters, cluster =>
            cluster.EntityId == "char_audrey" && cluster.Sentiment == "positive");
        Assert.Contains(clusters, cluster =>
            cluster.EntityId == "game:strinova" && cluster.Sentiment == "negative");
    }

    [Fact]
    public void Merge_preserves_existing_context_and_appends_scraped_clusters()
    {
        var context = new ContextBundle
        {
            GameDefinition = GameDefinition(),
            Rules = new Dictionary<string, EntityRules>(),
            UpdatePlan = new UpdatePlanSnapshot { Changes = Array.Empty<PlannedChange>() },
            Community = new CommunitySnapshot
            {
                Clusters =
                [
                    new CommunityCluster
                    {
                        EntityId = "char_audrey",
                        Theme = "survey_feedback",
                        Volume = 5,
                        Sentiment = "negative",
                    },
                ],
            },
            JoinedChanges = Array.Empty<JoinedChange>(),
        };
        var feedback = new List<CommunityFeedbackItem>
        {
            new("steam", "Strinova", "Audrey needs more range", "negative", "english", null),
        };

        ContextBundle result = _builder.Merge(context, feedback);

        Assert.Equal(2, result.Community.Clusters.Count);
        Assert.Same(context.GameDefinition, result.GameDefinition);
        Assert.Same(context.UpdatePlan, result.UpdatePlan);
    }

    private static GameDefinitionSnapshot GameDefinition() => new()
    {
        GameId = "strinova",
        Entities =
        [
            new GameEntity
            {
                Id = "char_audrey",
                Name = "Audrey",
            },
        ],
    };
}
