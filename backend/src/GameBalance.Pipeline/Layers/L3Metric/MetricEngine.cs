namespace GameBalance.Pipeline.Layers.L3Metric;

/// <summary>L3 - computes entity metrics by bracket and telemetry source.</summary>
public sealed class MetricEngine
{
    public MetricResult Compute(IReadOnlyList<Dictionary<string, object?>> events)
    {
        List<MetricEvent> normalized = events
            .Select((ev, index) => Normalize(ev, index))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();

        Dictionary<(string BracketId, string Source), int> totalMatches = normalized
            .GroupBy(item => (item.BracketId, item.Source))
            .ToDictionary(group => group.Key, CountBracketMatches);

        var metrics = new SortedDictionary<string, IReadOnlyDictionary<string, CohortMetrics>>(
            StringComparer.Ordinal);

        foreach (IGrouping<string, MetricEvent> entity in normalized.GroupBy(item => item.EntityId))
        {
            var brackets = new SortedDictionary<string, CohortMetrics>(StringComparer.Ordinal);
            foreach (IGrouping<string, MetricEvent> bracket in entity.GroupBy(item => item.BracketId))
            {
                var sources = new SortedDictionary<string, SourceMetrics>(StringComparer.Ordinal);
                foreach (IGrouping<string, MetricEvent> source in bracket.GroupBy(item => item.Source))
                {
                    sources[source.Key] = ComputeSource(
                        source.ToList(),
                        totalMatches[(bracket.Key, source.Key)]);
                }

                sources.TryGetValue("online", out SourceMetrics? online);
                sources.TryGetValue("offline", out SourceMetrics? offline);
                brackets[bracket.Key] = new CohortMetrics
                {
                    EntityId = entity.Key,
                    BracketId = bracket.Key,
                    Sources = sources,
                    Comparison = online is not null && offline is not null
                        ? Compare(online, offline)
                        : null,
                };
            }

            metrics[entity.Key] = brackets;
        }

        return new MetricResult { Metrics = metrics };
    }

    private static SourceMetrics ComputeSource(
        IReadOnlyList<MetricEvent> events,
        int bracketMatchCount)
    {
        List<MetricEvent> matchEnds = events.Where(item => item.EventType == "match_end").ToList();
        List<bool> outcomes = matchEnds
            .Select(item => ReadWinOutcome(item.Raw))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        List<MetricEvent> picks = events.Where(item => item.EventType == "entity_pick").ToList();
        int pickCount = CountDistinctMatchesOrEvents(picks);
        int deathCount = events.Count(item => item.EventType == "death");
        int entityMatchCount = CountDistinctMatchesOrEvents(events);

        return new SourceMetrics
        {
            EventCount = events.Count,
            MatchCount = entityMatchCount,
            PickCount = pickCount,
            DeathCount = deathCount,
            WinRate = outcomes.Count == 0 ? null : outcomes.Count(value => value) / (double)outcomes.Count,
            PickRate = bracketMatchCount == 0 ? null : pickCount / (double)bracketMatchCount,
            DeathRate = entityMatchCount == 0 ? null : deathCount / (double)entityMatchCount,
        };
    }

    private static int CountBracketMatches(IEnumerable<MetricEvent> events)
    {
        List<MetricEvent> values = events.ToList();
        HashSet<string> ids = values
            .Where(item => item.MatchId is not null)
            .Select(item => item.MatchId!)
            .ToHashSet(StringComparer.Ordinal);

        // Events without a match/session id can still establish a match through match_end.
        int anonymousMatchEnds = values.Count(item =>
            item.MatchId is null && item.EventType == "match_end");
        return ids.Count + anonymousMatchEnds;
    }

    private static int CountDistinctMatchesOrEvents(IEnumerable<MetricEvent> events)
    {
        List<MetricEvent> values = events.ToList();
        HashSet<string> ids = values
            .Where(item => item.MatchId is not null)
            .Select(item => item.MatchId!)
            .ToHashSet(StringComparer.Ordinal);

        if (ids.Count > 0)
        {
            return ids.Count;
        }

        int matchEnds = values.Count(item => item.EventType == "match_end");
        return matchEnds > 0 ? matchEnds : values.Count;
    }

    private static SourceComparison Compare(SourceMetrics online, SourceMetrics offline) => new()
    {
        WinRateDelta = Difference(offline.WinRate, online.WinRate),
        PickRateDelta = Difference(offline.PickRate, online.PickRate),
        DeathRateDelta = Difference(offline.DeathRate, online.DeathRate),
    };

    private static double? Difference(double? left, double? right) =>
        left.HasValue && right.HasValue ? left.Value - right.Value : null;

    private static MetricEvent? Normalize(Dictionary<string, object?> ev, int index)
    {
        string? entityId = ReadString(ev, "entity_id");
        string? bracketId = ReadString(ev, "bracket_id") ?? ReadString(ev, "bracket");
        string? source = NormalizeSource(ReadString(ev, "source"));
        string? eventType = ReadString(ev, "event_type");
        if (entityId is null || bracketId is null || source is null || eventType is null)
        {
            return null;
        }

        return new MetricEvent(
            index,
            entityId,
            bracketId,
            source,
            eventType,
            ReadString(ev, "match_id") ?? ReadString(ev, "session_id"),
            ev);
    }

    private static string? NormalizeSource(string? source) => source?.ToLowerInvariant() switch
    {
        "online" or "live" => "online",
        "offline" or "playtest" => "offline",
        _ => null,
    };

    private static bool? ReadWinOutcome(Dictionary<string, object?> ev)
    {
        if (ev.TryGetValue("won", out object? won) && won is bool boolean)
        {
            return boolean;
        }

        return ReadString(ev, "result")?.ToLowerInvariant() switch
        {
            "win" or "won" or "victory" => true,
            "loss" or "lost" or "defeat" => false,
            _ => null,
        };
    }

    private static string? ReadString(Dictionary<string, object?> ev, string key) =>
        ev.TryGetValue(key, out object? value) && value is not null &&
        !string.IsNullOrWhiteSpace(value.ToString())
            ? value.ToString()
            : null;

    private sealed record MetricEvent(
        int Index,
        string EntityId,
        string BracketId,
        string Source,
        string EventType,
        string? MatchId,
        Dictionary<string, object?> Raw);
}
