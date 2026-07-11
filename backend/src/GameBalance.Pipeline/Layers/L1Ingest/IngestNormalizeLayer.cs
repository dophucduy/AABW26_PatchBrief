namespace GameBalance.Pipeline.Layers.L1Ingest;

/// <summary>
/// L1 - Ingest &amp; Normalize.
///
/// Consumes the (already field-mapped) online and offline event streams from L0
/// and produces a single clean, validated, source-tagged event list
/// (Requirement 4):
/// <list type="number">
///   <item>Events from the online stream are tagged <c>source = "online"</c>.</item>
///   <item>Events from the offline stream are tagged <c>source = "offline"</c>.</item>
///   <item>Each event must carry a recognized <c>event_type</c>; unknown types
///         are discarded with a warning containing the index and type.</item>
///   <item>Each event must carry <c>timestamp</c> and <c>entity_id</c>; events
///         missing required fields are discarded and recorded in the summary.</item>
/// </list>
/// </summary>
public sealed class IngestNormalizeLayer
{
    private const string SourceField = "source";
    private const string EventTypeField = "event_type";
    private const string TimestampField = "timestamp";
    private const string EntityIdField = "entity_id";

    /// <summary>
    /// Normalize one selected telemetry stream and preserve that source tag for
    /// every downstream layer.
    /// </summary>
    public IngestResult Normalize(
        IReadOnlyList<Dictionary<string, object?>> events,
        EventSource source)
    {
        var accepted = new List<Dictionary<string, object?>>(events.Count);
        var discarded = new List<DiscardedEvent>();
        var warnings = new List<string>();

        Ingest(events, source, accepted, discarded, warnings);

        return new IngestResult
        {
            Events = accepted,
            Discarded = discarded,
            Warnings = warnings,
        };
    }

    /// <summary>
    /// Normalize both event streams into one validated, source-tagged list.
    /// </summary>
    public IngestResult Normalize(
        IReadOnlyList<Dictionary<string, object?>> onlineEvents,
        IReadOnlyList<Dictionary<string, object?>> offlineEvents)
    {
        var accepted = new List<Dictionary<string, object?>>(
            onlineEvents.Count + offlineEvents.Count);
        var discarded = new List<DiscardedEvent>();
        var warnings = new List<string>();

        Ingest(onlineEvents, EventSource.Online, accepted, discarded, warnings);
        Ingest(offlineEvents, EventSource.Offline, accepted, discarded, warnings);

        return new IngestResult
        {
            Events = accepted,
            Discarded = discarded,
            Warnings = warnings,
        };
    }

    private static void Ingest(
        IReadOnlyList<Dictionary<string, object?>> events,
        EventSource source,
        List<Dictionary<string, object?>> accepted,
        List<DiscardedEvent> discarded,
        List<string> warnings)
    {
        string sourceTag = source.ToTag();

        for (int index = 0; index < events.Count; index++)
        {
            Dictionary<string, object?> ev = events[index];
            string eventType = GetString(ev, EventTypeField) ?? "<missing>";

            // AC3/AC4: reject unrecognized event types.
            if (!EventTypes.IsSupported(GetString(ev, EventTypeField)))
            {
                discarded.Add(new DiscardedEvent(index, sourceTag, eventType, "unrecognized event_type"));
                warnings.Add($"[{sourceTag}] event {index}: unrecognized event_type '{eventType}'; discarded");
                continue;
            }

            // AC5/AC6: require timestamp and entity_id.
            var missing = new List<string>();
            if (!HasValue(ev, TimestampField))
            {
                missing.Add(TimestampField);
            }

            if (!HasValue(ev, EntityIdField))
            {
                missing.Add(EntityIdField);
            }

            if (missing.Count > 0)
            {
                string reason = $"missing required field(s): {string.Join(", ", missing)}";
                discarded.Add(new DiscardedEvent(index, sourceTag, eventType, reason));
                warnings.Add($"[{sourceTag}] event {index} ({eventType}): {reason}; discarded");
                continue;
            }

            // AC1/AC2: tag the source (defensive copy, do not mutate input).
            var normalized = new Dictionary<string, object?>(ev)
            {
                [SourceField] = sourceTag,
            };
            accepted.Add(normalized);
        }
    }

    private static bool HasValue(Dictionary<string, object?> ev, string key) =>
        ev.TryGetValue(key, out object? value) && value is not null &&
        !(value is string s && string.IsNullOrWhiteSpace(s));

    private static string? GetString(Dictionary<string, object?> ev, string key) =>
        ev.TryGetValue(key, out object? value) ? value?.ToString() : null;
}
