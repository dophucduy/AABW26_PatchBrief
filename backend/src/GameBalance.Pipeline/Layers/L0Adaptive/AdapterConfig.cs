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
///   "field_map": {
///     "player": "player_id",
///     "hero":   "entity_id",
///     "ts":     "timestamp",
///     "kind":   "event_type"
///   }
/// }
/// </code>
/// Keys are studio source field names; values are canonical field names.
/// </summary>
public sealed class AdapterConfig
{
    [JsonPropertyName("field_map")]
    public Dictionary<string, string> FieldMap { get; init; } = new();

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

        JsonElement mapElement = root;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("field_map", out JsonElement wrapped))
        {
            mapElement = wrapped;
        }

        if (mapElement.ValueKind != JsonValueKind.Object)
        {
            return Empty;
        }

        var map = new Dictionary<string, string>();
        foreach (JsonProperty prop in mapElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                map[prop.Name] = prop.Value.GetString()!;
            }
        }

        return new AdapterConfig { FieldMap = map };
    }
}
