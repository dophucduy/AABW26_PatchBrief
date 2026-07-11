using System.Globalization;

namespace GameBalance.Pipeline.Layers.L2Semantic;

/// <summary>L2 - cohort segmentation, behavior profiles and deterministic flags.</summary>
public sealed class SemanticAnalyzer
{
    public const int LowConfidenceEventThreshold = 5;
    public const int MinimumOneTrickPicks = 3;
    public const double OneTrickShareThreshold = 0.80;
    public const double MetaDominantShareThreshold = 0.50;
    public const double BracketSplitWinRateThreshold = 0.10;
    public const double BracketSplitPickShareThreshold = 0.20;

    public SemanticResult Analyze(
        IReadOnlyList<Dictionary<string, object?>> events,
        string gameDefinitionJson) =>
        Analyze(events, BracketDefinition.ParseGameDefinition(gameDefinitionJson));

    public SemanticResult Analyze(
        IReadOnlyList<Dictionary<string, object?>> events,
        IReadOnlyList<BracketDefinition> brackets)
    {
        var warnings = new List<string>();
        if (brackets.Count == 0)
        {
            warnings.Add("game_definition contains no valid bracket definitions");
            return new SemanticResult
            {
                BracketProfiles = Array.Empty<BracketBehaviorProfile>(),
                BracketEntities = Array.Empty<BracketEntitySummary>(),
                Patterns = Array.Empty<BehaviorPattern>(),
                Warnings = warnings,
            };
        }

        var segmented = new List<SegmentedEvent>(events.Count);
        for (int index = 0; index < events.Count; index++)
        {
            string? bracketId = ResolveBracket(events[index], brackets);
            if (bracketId is null)
            {
                warnings.Add($"event {index} could not be assigned to a configured bracket; skipped");
                continue;
            }

            segmented.Add(new SegmentedEvent(index, bracketId, events[index]));
        }

        IReadOnlyList<BracketBehaviorProfile> profiles = BuildProfiles(segmented, brackets);
        IReadOnlyList<BracketEntitySummary> combinations = BuildCombinations(segmented, profiles);
        var patterns = new List<BehaviorPattern>();
        patterns.AddRange(FindOneTricks(segmented));
        patterns.AddRange(FindMetaDominant(segmented, combinations));
        patterns.AddRange(FindBracketSplits(combinations));

        return new SemanticResult
        {
            BracketProfiles = profiles,
            BracketEntities = combinations,
            Patterns = patterns,
            Warnings = warnings,
        };
    }

    private static IReadOnlyList<BracketBehaviorProfile> BuildProfiles(
        IReadOnlyList<SegmentedEvent> events,
        IReadOnlyList<BracketDefinition> brackets)
    {
        var result = new List<BracketBehaviorProfile>();
        foreach (BracketDefinition bracket in brackets)
        {
            List<SegmentedEvent> cohort = events.Where(e => e.BracketId == bracket.Id).ToList();
            Dictionary<string, int> playPatterns = cohort
                .Select(e => ReadString(e.Event, "event_type"))
                .Where(type => type is not null)
                .GroupBy(type => type!)
                .ToDictionary(group => group.Key, group => group.Count());

            List<SegmentedEvent> picks = cohort
                .Where(e => ReadString(e.Event, "event_type") == "entity_pick")
                .ToList();
            int totalPicks = picks.Count;
            List<EntityPreference> preferences = picks
                .Select(e => ReadString(e.Event, "entity_id"))
                .Where(id => id is not null)
                .GroupBy(id => id!)
                .Select(group => new EntityPreference(
                    group.Key,
                    group.Count(),
                    totalPicks == 0 ? 0 : (double)group.Count() / totalPicks))
                .OrderByDescending(item => item.PickShare)
                .ThenBy(item => item.EntityId, StringComparer.Ordinal)
                .ToList();

            int playerCount = cohort
                .Select(e => ReadString(e.Event, "player_id"))
                .Where(id => id is not null)
                .Distinct(StringComparer.Ordinal)
                .Count();

            result.Add(new BracketBehaviorProfile
            {
                BracketId = bracket.Id,
                EventCount = cohort.Count,
                PlayerCount = playerCount,
                PlayPatterns = playPatterns,
                EntityPreferences = preferences,
            });
        }

        return result;
    }

    private static IReadOnlyList<BracketEntitySummary> BuildCombinations(
        IReadOnlyList<SegmentedEvent> events,
        IReadOnlyList<BracketBehaviorProfile> profiles)
    {
        var pickShares = profiles.ToDictionary(
            profile => profile.BracketId,
            profile => profile.EntityPreferences.ToDictionary(x => x.EntityId, x => x.PickShare));

        return events
            .Select(e => new { Event = e, EntityId = ReadString(e.Event, "entity_id") })
            .Where(x => x.EntityId is not null)
            .GroupBy(x => (x.Event.BracketId, EntityId: x.EntityId!))
            .Select(group =>
            {
                List<Dictionary<string, object?>> matchEnds = group
                    .Where(x => ReadString(x.Event.Event, "event_type") == "match_end")
                    .Select(x => x.Event.Event)
                    .ToList();
                List<bool> outcomes = matchEnds
                    .Select(ReadWinOutcome)
                    .Where(value => value.HasValue)
                    .Select(value => value!.Value)
                    .ToList();

                return new BracketEntitySummary
                {
                    BracketId = group.Key.BracketId,
                    EntityId = group.Key.EntityId,
                    EventCount = group.Count(),
                    PickShare = pickShares[group.Key.BracketId]
                        .GetValueOrDefault(group.Key.EntityId, 0),
                    WinRate = outcomes.Count == 0 ? null : outcomes.Count(value => value) / (double)outcomes.Count,
                    LowConfidence = group.Count() < LowConfidenceEventThreshold,
                };
            })
            .OrderBy(item => item.BracketId, StringComparer.Ordinal)
            .ThenBy(item => item.EntityId, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<BehaviorPattern> FindOneTricks(IReadOnlyList<SegmentedEvent> events)
    {
        var picks = events.Where(e =>
            ReadString(e.Event, "event_type") == "entity_pick" &&
            ReadString(e.Event, "player_id") is not null &&
            ReadString(e.Event, "entity_id") is not null);

        foreach (var player in picks.GroupBy(e =>
                     (e.BracketId, PlayerId: ReadString(e.Event, "player_id")!)))
        {
            int total = player.Count();
            var favorite = player
                .GroupBy(e => ReadString(e.Event, "entity_id")!)
                .Select(group => new { EntityId = group.Key, Count = group.Count() })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.EntityId, StringComparer.Ordinal)
                .First();
            double share = favorite.Count / (double)total;
            if (total >= MinimumOneTrickPicks && share >= OneTrickShareThreshold)
            {
                yield return new BehaviorPattern
                {
                    Type = "one_trick",
                    BracketId = player.Key.BracketId,
                    EntityId = favorite.EntityId,
                    PlayerId = player.Key.PlayerId,
                    Details = $"player used {favorite.EntityId} for {share:P0} of picks",
                    Evidence = new[] { $"favorite_picks:{favorite.Count}", $"total_picks:{total}" },
                    LowConfidence = total < LowConfidenceEventThreshold,
                };
            }
        }
    }

    private static IEnumerable<BehaviorPattern> FindMetaDominant(
        IReadOnlyList<SegmentedEvent> events,
        IReadOnlyList<BracketEntitySummary> combinations)
    {
        foreach (IGrouping<string, SegmentedEvent> bracket in events.GroupBy(e => e.BracketId))
        {
            HashSet<string> allMatches = bracket
                .Select(GetMatchId)
                .Where(id => id is not null)
                .Select(id => id!)
                .ToHashSet(StringComparer.Ordinal);
            if (allMatches.Count == 0)
            {
                continue;
            }

            var entityMatches = bracket
                .Where(e => ReadString(e.Event, "event_type") == "entity_pick")
                .Select(e => new { EntityId = ReadString(e.Event, "entity_id"), MatchId = GetMatchId(e) })
                .Where(x => x.EntityId is not null && x.MatchId is not null)
                .GroupBy(x => x.EntityId!);

            foreach (var entity in entityMatches)
            {
                int pickedMatches = entity.Select(x => x.MatchId!).Distinct(StringComparer.Ordinal).Count();
                double share = pickedMatches / (double)allMatches.Count;
                if (share > MetaDominantShareThreshold)
                {
                    BracketEntitySummary? combination = combinations.FirstOrDefault(x =>
                        x.BracketId == bracket.Key && x.EntityId == entity.Key);
                    yield return new BehaviorPattern
                    {
                        Type = "meta_dominant",
                        BracketId = bracket.Key,
                        EntityId = entity.Key,
                        Details = $"entity picked in {share:P0} of matches",
                        Evidence = new[] { $"picked_matches:{pickedMatches}", $"bracket_matches:{allMatches.Count}" },
                        LowConfidence = combination?.LowConfidence ?? true,
                    };
                }
            }
        }
    }

    private static IEnumerable<BehaviorPattern> FindBracketSplits(
        IReadOnlyList<BracketEntitySummary> combinations)
    {
        foreach (IGrouping<string, BracketEntitySummary> entity in combinations.GroupBy(x => x.EntityId))
        {
            List<BracketEntitySummary> values = entity.ToList();
            if (values.Count < 2)
            {
                continue;
            }

            List<BracketEntitySummary> withWinRate = values.Where(x => x.WinRate.HasValue).ToList();
            double winGap = withWinRate.Count < 2
                ? 0
                : withWinRate.Max(x => x.WinRate!.Value) - withWinRate.Min(x => x.WinRate!.Value);
            double pickGap = values.Max(x => x.PickShare) - values.Min(x => x.PickShare);
            if (winGap < BracketSplitWinRateThreshold && pickGap < BracketSplitPickShareThreshold)
            {
                continue;
            }

            yield return new BehaviorPattern
            {
                Type = "bracket_split",
                EntityId = entity.Key,
                Details = "entity performance or preference differs across brackets",
                Evidence = new[]
                {
                    $"win_rate_gap:{winGap.ToString("0.###", CultureInfo.InvariantCulture)}",
                    $"pick_share_gap:{pickGap.ToString("0.###", CultureInfo.InvariantCulture)}",
                },
                LowConfidence = values.Any(x => x.LowConfidence),
            };
        }
    }

    private static string? ResolveBracket(
        Dictionary<string, object?> ev,
        IReadOnlyList<BracketDefinition> brackets)
    {
        string? explicitId = ReadString(ev, "bracket_id") ?? ReadString(ev, "bracket");
        if (explicitId is not null)
        {
            return brackets.Any(b => b.Id == explicitId) ? explicitId : null;
        }

        if (TryReadDouble(ev, "skill_rating", out double rating))
        {
            return brackets.FirstOrDefault(b => b.Contains(rating))?.Id;
        }

        return null;
    }

    private static string? GetMatchId(SegmentedEvent item) =>
        ReadString(item.Event, "match_id") ?? ReadString(item.Event, "session_id");

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

    private static bool TryReadDouble(Dictionary<string, object?> ev, string key, out double value)
    {
        value = default;
        return ev.TryGetValue(key, out object? raw) && raw is not null &&
               double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private sealed record SegmentedEvent(
        int Index,
        string BracketId,
        Dictionary<string, object?> Event);
}
