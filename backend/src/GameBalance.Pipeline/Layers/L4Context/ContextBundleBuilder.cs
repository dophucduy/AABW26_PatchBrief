using System.Text.Json;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L3Metric;

namespace GameBalance.Pipeline.Layers.L4Context;

/// <summary>
/// Builds the canonical L4 ContextBundle from its four source documents and
/// validates entity references before L5-L7 consume the result.
/// </summary>
public sealed class ContextBundleBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ContextBundle Build(
        string gameDefinitionJson,
        string rulesJson,
        string updatePlanJson,
        string communityJson,
        MetricResult? metrics = null)
    {
        GameDefinitionSnapshot gameDefinition = Parse<GameDefinitionSnapshot>(
            gameDefinitionJson,
            "game_definition.json");
        IReadOnlyDictionary<string, EntityRules> rules =
            Parse<Dictionary<string, EntityRules>>(rulesJson, "rules.json");
        UpdatePlanSnapshot updatePlan = Parse<UpdatePlanSnapshot>(
            updatePlanJson,
            "update_plan.json");
        CommunitySnapshot community = Parse<CommunitySnapshot>(
            communityJson,
            "community.json");

        Dictionary<string, GameEntity> entities = ValidateGameDefinition(gameDefinition);
        ValidateRules(rules, entities);
        ValidateUpdatePlan(updatePlan, entities);

        var warnings = new List<string>();
        AddCommunityWarnings(community, entities, warnings);
        AddMetricWarnings(metrics, entities, warnings);

        IReadOnlyList<JoinedChange> joinedChanges = updatePlan.Changes
            .Select(change => JoinChange(change, entities[change.Target], rules))
            .ToList();

        return new ContextBundle
        {
            GameDefinition = gameDefinition,
            Rules = rules,
            UpdatePlan = updatePlan,
            Community = community,
            JoinedChanges = joinedChanges,
            Warnings = warnings,
        };
    }

    private static T Parse<T>(string json, string fileName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException($"{fileName} is empty");
        }

        try
        {
            T? value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            return value ?? throw new InvalidDataException($"{fileName} could not be parsed");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"{fileName} contains invalid JSON: {exception.Message}",
                exception);
        }
    }

    private static Dictionary<string, GameEntity> ValidateGameDefinition(
        GameDefinitionSnapshot gameDefinition)
    {
        if (gameDefinition.Entities is null || gameDefinition.Entities.Count == 0)
        {
            throw new InvalidDataException("game_definition.json must contain at least one entity");
        }

        var entities = new Dictionary<string, GameEntity>(StringComparer.Ordinal);
        foreach (GameEntity entity in gameDefinition.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Id) || string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new InvalidDataException(
                    "every game_definition entity must contain non-empty id and name");
            }

            if (!entities.TryAdd(entity.Id, entity))
            {
                throw new InvalidDataException(
                    $"game_definition.json contains duplicate entity id '{entity.Id}'");
            }
        }

        return entities;
    }

    private static void ValidateRules(
        IReadOnlyDictionary<string, EntityRules> rules,
        IReadOnlyDictionary<string, GameEntity> entities)
    {
        string[] unresolved = rules.Keys
            .Where(entityId => !entities.ContainsKey(entityId))
            .OrderBy(entityId => entityId, StringComparer.Ordinal)
            .ToArray();
        if (unresolved.Length > 0)
        {
            throw new InvalidDataException(
                $"rules.json references unknown entity IDs: {string.Join(", ", unresolved)}");
        }
    }

    private static void ValidateUpdatePlan(
        UpdatePlanSnapshot updatePlan,
        IReadOnlyDictionary<string, GameEntity> entities)
    {
        if (updatePlan.Changes is null)
        {
            throw new InvalidDataException("update_plan.json must contain a changes array");
        }

        string[] unresolved = updatePlan.Changes
            .Select(change => change.Target)
            .Where(entityId => string.IsNullOrWhiteSpace(entityId) || !entities.ContainsKey(entityId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(entityId => entityId, StringComparer.Ordinal)
            .ToArray();
        if (unresolved.Length > 0)
        {
            throw new InvalidDataException(
                $"update_plan.json references unknown entity IDs: {string.Join(", ", unresolved)}");
        }
    }

    private static void AddCommunityWarnings(
        CommunitySnapshot community,
        IReadOnlyDictionary<string, GameEntity> entities,
        ICollection<string> warnings)
    {
        if (community.Clusters is null)
        {
            throw new InvalidDataException("community.json must contain a clusters array");
        }

        foreach (string entityId in community.Clusters
                     .Select(cluster => cluster.EntityId)
                     .Where(entityId =>
                         !string.IsNullOrWhiteSpace(entityId) &&
                         !entityId.StartsWith("game:", StringComparison.Ordinal) &&
                         !entities.ContainsKey(entityId))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(entityId => entityId, StringComparer.Ordinal))
        {
            warnings.Add($"community cluster references unknown entity '{entityId}'");
        }
    }

    private static void AddMetricWarnings(
        MetricResult? metrics,
        IReadOnlyDictionary<string, GameEntity> entities,
        ICollection<string> warnings)
    {
        if (metrics is null)
        {
            return;
        }

        foreach (string entityId in metrics.Metrics.Keys
                     .Where(entityId => !entities.ContainsKey(entityId))
                     .OrderBy(entityId => entityId, StringComparer.Ordinal))
        {
            warnings.Add($"metrics references unknown entity '{entityId}'");
        }
    }

    private static JoinedChange JoinChange(
        PlannedChange change,
        GameEntity entity,
        IReadOnlyDictionary<string, EntityRules> rules)
    {
        rules.TryGetValue(change.Target, out EntityRules? entityRules);
        string field = NormalizeField(change.Field);
        string leverStatus = entityRules is null
            ? "unlisted"
            : entityRules.Locked.Any(item => NormalizeField(item) == field)
                ? "locked"
                : entityRules.Open.Any(item => NormalizeField(item) == field)
                    ? "open"
                    : "unlisted";

        return new JoinedChange
        {
            EntityId = entity.Id,
            EntityName = entity.Name,
            Role = entity.Role,
            Field = field,
            From = change.From,
            To = change.To,
            Delta = change.Delta,
            LeverStatus = leverStatus,
        };
    }

    private static string NormalizeField(string field) =>
        field.StartsWith("stats.", StringComparison.OrdinalIgnoreCase)
            ? field["stats.".Length..]
            : field;
}
