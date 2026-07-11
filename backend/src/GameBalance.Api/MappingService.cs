using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameBalance.Api;

internal sealed class MappingService
{
    private static readonly string[] CanonicalFields =
    [
        "entity_id", "timestamp", "event_type", "session_id", "bracket", "area_id", "cause_id", "source",
        "win_rate", "pick_rate", "finish_time", "death_rate", "death", "match_end", "ability_used",
        "entity_pick", "area_enter", "session_start",
    ];

    private static readonly Dictionary<string, string> FieldAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["characterid"] = "entity_id",
        ["character_id"] = "entity_id",
        ["hero"] = "entity_id",
        ["heroid"] = "entity_id",
        ["hero_id"] = "entity_id",
        ["gametime"] = "timestamp",
        ["game_time"] = "timestamp",
        ["time"] = "timestamp",
        ["playerrank"] = "bracket",
        ["player_rank"] = "bracket",
        ["rank"] = "bracket",
        ["bracket_id"] = "bracket",
        ["sessionid"] = "session_id",
        ["session_id"] = "session_id",
        ["event"] = "event_type",
        ["eventname"] = "event_type",
        ["event_name"] = "event_type",
        ["eventtype"] = "event_type",
        ["wr"] = "win_rate",
        ["pr"] = "pick_rate",
        ["dr"] = "death_rate",
    };

    private static readonly Dictionary<string, string> EventAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["death"] = "death",
        ["playerdeath"] = "death",
        ["matchend"] = "match_end",
        ["match_end"] = "match_end",
        ["abilityused"] = "ability_used",
        ["entitypick"] = "entity_pick",
        ["areaenter"] = "area_enter",
        ["sessionstart"] = "session_start",
    };

    public MappingSuggestionResponse Suggest(string sampleJson, string? genre)
    {
        IReadOnlyList<Dictionary<string, object?>> events = ParseSampleEvents(sampleJson);
        var fieldSuggestions = new List<MappingEntryResponse>();
        var eventSuggestions = new List<MappingEntryResponse>();

        if (events.Count == 0)
        {
            return new MappingSuggestionResponse
            {
                FieldMap = fieldSuggestions,
                EventMap = eventSuggestions,
            };
        }

        HashSet<string> keys = events
            .SelectMany(ev => ev.Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string key in keys.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            if (IsEventTypeKey(key))
            {
                continue;
            }

            string? target = ResolveFieldTarget(key);
            if (target is null)
            {
                continue;
            }

            fieldSuggestions.Add(new MappingEntryResponse
            {
                Source = key,
                Target = target,
                Confidence = ConfidenceForField(key, target, genre),
            });
        }

        string eventKey = keys.FirstOrDefault(IsEventTypeKey) ?? "event_type";
        HashSet<string> eventValues = events
            .Select(ev => ReadEventValue(ev, eventKey))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string eventValue in eventValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            string? target = ResolveEventTarget(eventValue);
            if (target is null)
            {
                continue;
            }

            eventSuggestions.Add(new MappingEntryResponse
            {
                Source = eventValue,
                Target = target,
                Confidence = ConfidenceForEvent(eventValue, target),
            });
        }

        return new MappingSuggestionResponse
        {
            FieldMap = fieldSuggestions,
            EventMap = eventSuggestions,
        };
    }

    public MappingPreviewResponse Preview(
        string sampleJson,
        IReadOnlyList<MappingRowRequest> fieldMap,
        IReadOnlyList<MappingRowRequest> eventMap)
    {
        IReadOnlyList<Dictionary<string, object?>> events = ParseSampleEvents(sampleJson);
        var fieldDictionary = fieldMap.ToDictionary(row => row.Source, row => row.Target, StringComparer.OrdinalIgnoreCase);
        var eventDictionary = eventMap.ToDictionary(row => row.Source, row => row.Target, StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        int parsed = 0;
        int skipped = 0;

        foreach (Dictionary<string, object?> ev in events)
        {
            string? eventValue = ReadEventValue(ev, "event_type")
                ?? ReadEventValue(ev, "event")
                ?? ReadEventValue(ev, "event_name");

            if (!string.IsNullOrWhiteSpace(eventValue) &&
                eventDictionary.TryGetValue(eventValue, out string? mappedEvent))
            {
                eventValue = mappedEvent;
            }

            var adapted = new Dictionary<string, object?>(ev);
            foreach ((string source, string target) in fieldDictionary)
            {
                if (adapted.TryGetValue(source, out object? value))
                {
                    adapted.Remove(source);
                    adapted[target] = value;
                }
            }

            if (!string.IsNullOrWhiteSpace(eventValue))
            {
                adapted["event_type"] = eventValue;
            }

            bool hasEntity = adapted.ContainsKey("entity_id");
            bool hasTimestamp = adapted.ContainsKey("timestamp");
            if (!hasEntity || !hasTimestamp)
            {
                skipped++;
                if (!hasTimestamp)
                {
                    warnings.Add("Some rows are missing the timestamp field.");
                }

                continue;
            }

            parsed++;
        }

        return new MappingPreviewResponse
        {
            EventsParsed = parsed,
            EventsSkipped = skipped,
            Warnings = warnings.Distinct().ToList(),
        };
    }

    private static IReadOnlyList<Dictionary<string, object?>> ParseSampleEvents(string sampleJson)
    {
        using JsonDocument document = JsonDocument.Parse(sampleJson);
        JsonElement root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("events", out JsonElement wrapped))
            {
                return ReadEventArray(wrapped);
            }

            if (root.TryGetProperty("telemetry", out JsonElement telemetry))
            {
                return ReadEventArray(telemetry);
            }
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            return ReadEventArray(root);
        }

        return Array.Empty<Dictionary<string, object?>>();
    }

    private static List<Dictionary<string, object?>> ReadEventArray(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var events = new List<Dictionary<string, object?>>();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            events.Add(item.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertValue(property.Value)));
        }

        return events;
    }

    private static object? ConvertValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        _ => value.ToString(),
    };

    private static bool IsEventTypeKey(string key) =>
        key.Equals("event_type", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("event", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("event_name", StringComparison.OrdinalIgnoreCase);

    private static string? ReadEventValue(Dictionary<string, object?> ev, string key) =>
        ev.TryGetValue(key, out object? value) ? value?.ToString() : null;

    private static string? ResolveFieldTarget(string source)
    {
        if (FieldAliases.TryGetValue(source, out string? alias))
        {
            return alias;
        }

        if (CanonicalFields.Contains(source, StringComparer.OrdinalIgnoreCase))
        {
            return source.ToLowerInvariant();
        }

        return null;
    }

    private static string? ResolveEventTarget(string source) =>
        EventAliases.TryGetValue(source, out string? alias) ? alias : source.ToLowerInvariant();

    private static double ConfidenceForField(string source, string target, string? genre)
    {
        if (FieldAliases.TryGetValue(source, out string? alias) && alias == target)
        {
            return 0.92;
        }

        if (CanonicalFields.Contains(source, StringComparer.OrdinalIgnoreCase))
        {
            return 0.95;
        }

        return string.IsNullOrWhiteSpace(genre) ? 0.78 : 0.82;
    }

    private static double ConfidenceForEvent(string source, string target) =>
        EventAliases.TryGetValue(source, out string? alias) && alias == target ? 0.91 : 0.84;
}

internal sealed class MappingSuggestionResponse
{
    [JsonPropertyName("field_map")]
    public required List<MappingEntryResponse> FieldMap { get; init; }

    [JsonPropertyName("event_map")]
    public required List<MappingEntryResponse> EventMap { get; init; }
}

internal sealed class MappingEntryResponse
{
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }
}

internal sealed class MappingPreviewResponse
{
    [JsonPropertyName("events_parsed")]
    public int EventsParsed { get; init; }

    [JsonPropertyName("events_skipped")]
    public int EventsSkipped { get; init; }

    [JsonPropertyName("warnings")]
    public required List<string> Warnings { get; init; }
}

internal sealed class MappingRowRequest
{
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }
}
