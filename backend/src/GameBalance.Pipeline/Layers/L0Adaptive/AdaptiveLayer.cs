namespace GameBalance.Pipeline.Layers.L0Adaptive;

/// <summary>
/// L0 - Adaptive Layer.
///
/// Maps studio-specific telemetry field names to the canonical pipeline schema
/// (Requirement 3):
/// <list type="number">
///   <item>When an adapter is provided, rename source fields to canonical.</item>
///   <item>When no adapter is provided, pass events through unchanged.</item>
///   <item>If a mapping references a field absent from every event, warn and
///         skip that mapping without failing.</item>
///   <item>Fields not mentioned in the adapter pass through unchanged.</item>
/// </list>
/// </summary>
public sealed class AdaptiveLayer
{
    /// <summary>Adapt a single event stream.</summary>
    public AdapterResult Adapt(
        IReadOnlyList<Dictionary<string, object?>> events,
        AdapterConfig? adapter)
    {
        Dictionary<string, string> fieldMap = adapter?.FieldMap ?? AdapterConfig.Empty.FieldMap;

        // AC2: no adapter (or empty map) => canonical passthrough (defensive copy).
        if (fieldMap.Count == 0)
        {
            var passthrough = events
                .Select(e => new Dictionary<string, object?>(e))
                .ToList();
            return new AdapterResult { Events = passthrough };
        }

        var adapted = new List<Dictionary<string, object?>>(events.Count);
        foreach (Dictionary<string, object?> ev in events)
        {
            adapted.Add(AdaptEvent(ev, fieldMap));
        }

        // AC3: a mapping is only "missing" if no event contained its source field.
        var warnings = new List<string>();
        foreach ((string source, string canonical) in fieldMap)
        {
            bool seen = events.Any(e => e.ContainsKey(source));
            if (!seen)
            {
                warnings.Add(
                    $"adapter maps '{source}' -> '{canonical}' but no event " +
                    $"contains field '{source}'; mapping skipped");
            }
        }

        return new AdapterResult { Events = adapted, Warnings = warnings };
    }

    /// <summary>Adapt both the online and offline event streams.</summary>
    public (AdapterResult Online, AdapterResult Offline) Apply(
        IReadOnlyList<Dictionary<string, object?>> onlineEvents,
        IReadOnlyList<Dictionary<string, object?>> offlineEvents,
        AdapterConfig? adapter)
    {
        return (Adapt(onlineEvents, adapter), Adapt(offlineEvents, adapter));
    }

    /// <summary>Adapt a structured telemetry export (win/pick/economy aggregates).</summary>
    public TelemetryAdapterResult AdaptTelemetry(string telemetryJson, AdapterConfig? adapter)
    {
        List<Dictionary<string, object?>> rows = TelemetryFormats.Parse(telemetryJson);
        Dictionary<string, string> metricMap = adapter?.MetricMap ?? AdapterConfig.Empty.MetricMap;

        if (metricMap.Count == 0)
        {
            var passthrough = rows
                .Select(row => new Dictionary<string, object?>(row))
                .ToList();
            return new TelemetryAdapterResult { Records = passthrough };
        }

        var adapted = new List<Dictionary<string, object?>>(rows.Count);
        foreach (Dictionary<string, object?> row in rows)
        {
            adapted.Add(AdaptRow(row, metricMap));
        }

        var warnings = new List<string>();
        foreach ((string source, string canonical) in metricMap)
        {
            bool seen = rows.Any(row => row.ContainsKey(source));
            if (!seen)
            {
                warnings.Add(
                    $"adapter maps '{source}' -> '{canonical}' but no telemetry row " +
                    $"contains field '{source}'; mapping skipped");
            }
        }

        return new TelemetryAdapterResult { Records = adapted, Warnings = warnings };
    }

    /// <summary>Adapt live and playtest telemetry exports.</summary>
    public (TelemetryAdapterResult Live, TelemetryAdapterResult Playtest) ApplyTelemetry(
        string liveTelemetryJson,
        string playtestTelemetryJson,
        AdapterConfig? adapter) =>
        (AdaptTelemetry(liveTelemetryJson, adapter), AdaptTelemetry(playtestTelemetryJson, adapter));

    private static Dictionary<string, object?> AdaptRow(
        Dictionary<string, object?> row,
        Dictionary<string, string> metricMap)
    {
        var result = new Dictionary<string, object?>(row.Count);
        foreach ((string key, object? value) in row)
        {
            string outKey = metricMap.TryGetValue(key, out string? canonical) ? canonical : key;
            result[outKey] = value;
        }

        return result;
    }

    private static Dictionary<string, object?> AdaptEvent(
        Dictionary<string, object?> ev,
        Dictionary<string, string> fieldMap)
    {
        var result = new Dictionary<string, object?>(ev.Count);
        foreach ((string key, object? value) in ev)
        {
            // AC1: rename mapped fields; AC4: pass through unmapped fields.
            string outKey = fieldMap.TryGetValue(key, out string? canonical) ? canonical : key;
            result[outKey] = value;
        }

        return result;
    }
}
