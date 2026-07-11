using System.Text.Json;

namespace GameBalance.Pipeline.Layers.L0Adaptive;

/// <summary>Parses studio telemetry exports into flat metric rows before L0 mapping.</summary>
internal static class TelemetryFormats
{
    public static List<Dictionary<string, object?>> Parse(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        return root.ValueKind switch
        {
            JsonValueKind.Array => ParseArray(root),
            JsonValueKind.Object => ParseObject(root),
            _ => throw new JsonException("telemetry must be a JSON array or object"),
        };
    }

    private static List<Dictionary<string, object?>> ParseObject(JsonElement root)
    {
        if (root.TryGetProperty("telemetry", out JsonElement telemetry))
        {
            return ParseArray(telemetry);
        }

        if (root.TryGetProperty("entities", out JsonElement entities))
        {
            return FlattenEntities(entities);
        }

        if (root.TryGetProperty("metrics", out JsonElement metrics))
        {
            return FlattenEntityMap(metrics);
        }

        throw new JsonException(
            "telemetry object must contain 'telemetry', 'entities', or 'metrics'");
    }

    private static List<Dictionary<string, object?>> ParseArray(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("telemetry list must be a JSON array");
        }

        var rows = new List<Dictionary<string, object?>>();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException($"telemetry row at index {rows.Count} is not a JSON object");
            }

            rows.Add(ConvertObject(item));
        }

        return rows;
    }

    private static List<Dictionary<string, object?>> FlattenEntities(JsonElement entities)
    {
        if (entities.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("'entities' must be a JSON array");
        }

        var rows = new List<Dictionary<string, object?>>();
        foreach (JsonElement entity in entities.EnumerateArray())
        {
            if (entity.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("each entity entry must be a JSON object");
            }

            string? entityId = ReadString(entity, "entity_id") ?? ReadString(entity, "id");
            if (entityId is null)
            {
                throw new JsonException("entity entry is missing 'entity_id'");
            }

            if (!entity.TryGetProperty("brackets", out JsonElement brackets) ||
                brackets.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException($"entity '{entityId}' is missing a 'brackets' array");
            }

            foreach (JsonElement bracket in brackets.EnumerateArray())
            {
                if (bracket.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"bracket row for '{entityId}' is not a JSON object");
                }

                var row = ConvertObject(bracket);
                row["entity_id"] = entityId;
                rows.Add(row);
            }
        }

        return rows;
    }

    private static List<Dictionary<string, object?>> FlattenEntityMap(JsonElement metrics)
    {
        if (metrics.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("'metrics' must be a JSON object keyed by entity_id");
        }

        var rows = new List<Dictionary<string, object?>>();
        foreach (JsonProperty entityProperty in metrics.EnumerateObject())
        {
            if (entityProperty.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string entityId = entityProperty.Name;
            JsonElement entity = entityProperty.Value;

            if (entity.TryGetProperty("brackets", out JsonElement brackets) &&
                brackets.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement bracket in brackets.EnumerateArray())
                {
                    var row = ConvertObject(bracket);
                    row["entity_id"] = entityId;
                    rows.Add(row);
                }

                continue;
            }

            foreach (JsonProperty sourceProperty in entity.EnumerateObject())
            {
                if (sourceProperty.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var row = ConvertObject(sourceProperty.Value);
                row["entity_id"] = entityId;
                row["source"] = sourceProperty.Name;
                rows.Add(row);
            }
        }

        return rows;
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static Dictionary<string, object?> ConvertObject(JsonElement element) =>
        element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => ConvertValue(property.Value));

    private static object? ConvertValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.Object => ConvertObject(value),
        JsonValueKind.Array => value.EnumerateArray().Select(ConvertValue).ToList(),
        _ => value.ToString(),
    };
}
