using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameBalance.Pipeline.Layers.L0Adaptive;

/// <summary>
/// Adapter configuration (adapter.json). Maps studio-specific telemetry field
/// names to the canonical pipeline schema.
///
/// Shape:
/// <code>
/// {
///   "metric_map": {
///     "hero":    "entity_id",
///     "bracket": "bracket_id",
///     "wr":      "win_rate",
///     "pr":      "pick_rate"
///   }
/// }
/// </code>
/// Keys are studio source field names; values are canonical field names.
/// </summary>
public sealed class AdapterConfig
{
    [JsonPropertyName("field_map")]
    public Dictionary<string, string> FieldMap { get; init; } = new();

    [JsonPropertyName("metric_map")]
    public Dictionary<string, string> MetricMap { get; init; } = new();

    /// <summary>An empty adapter with no mappings (canonical passthrough).</summary>
    public static AdapterConfig Empty { get; } = new();

    /// <summary>
    /// Parse an adapter from JSON. Accepts either the wrapped
    /// <c>{"field_map": {...}}</c> shape or a bare <c>{...}</c> mapping.
    /// Returns <see cref="Empty"/> when <paramref name="json"/> is null/blank.
    /// </summary>
    public static AdapterConfig Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty;
        }

        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        Dictionary<string, string> fieldMap = ReadStringMap(root, "field_map");
        Dictionary<string, string> metricMap = ReadStringMap(root, "metric_map");

        if (fieldMap.Count == 0 && metricMap.Count == 0 && root.ValueKind == JsonValueKind.Object)
        {
            fieldMap = ReadStringMap(root, null);
        }

        return new AdapterConfig { FieldMap = fieldMap, MetricMap = metricMap };
    }

    private static Dictionary<string, string> ReadStringMap(JsonElement root, string? propertyName)
    {
        JsonElement mapElement = root;
        if (propertyName is not null)
        {
            if (!root.TryGetProperty(propertyName, out mapElement))
            {
                return new Dictionary<string, string>();
            }
        }

        if (mapElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>();
        }

        var map = new Dictionary<string, string>();
        foreach (JsonProperty prop in mapElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                map[prop.Name] = prop.Value.GetString()!;
            }
        }

        return map;
    }
}
