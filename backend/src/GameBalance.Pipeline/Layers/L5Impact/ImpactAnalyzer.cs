using System.Text.Json;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Contracts;

namespace GameBalance.Pipeline.Layers.L5Impact;

/// <summary>L5 - rule-based impact and alignment analysis from metrics + context.</summary>
public sealed class ImpactAnalyzer
{
    private const double BracketSplitThreshold = 0.08;
    private const double NearAverageMin = 0.45;
    private const double NearAverageMax = 0.55;
    private const double PlaytestMismatchThreshold = 0.08;
    private const double StrongWinRate = 0.55;
    private const double WeakWinRate = 0.45;

    public ImpactResult Analyze(MetricResult metrics, ContextBundle context)
    {
        var patterns = new List<ImpactPattern>();
        var affected = new List<AffectedCohort>();
        var dataCommunityScores = new List<int>();
        var playtestLiveScores = new List<int>();

        BracketPair brackets = ResolveBrackets(context.GameDefinition.Brackets);

        foreach (JoinedChange change in context.JoinedChanges)
        {
            if (!metrics.Metrics.TryGetValue(change.EntityId, out IReadOnlyDictionary<string, CohortMetrics>? cohorts))
            {
                continue;
            }

            double? winRateLow = ReadOnlineWinRate(cohorts, brackets.LowBracketId);
            double? winRateHigh = ReadOnlineWinRate(cohorts, brackets.HighBracketId);
            double? pickRateLow = ReadOnlinePickRate(cohorts, brackets.LowBracketId);
            double? playtestDelta = ReadPlaytestDelta(cohorts, brackets.LowBracketId)
                ?? ReadPlaytestDelta(cohorts, brackets.HighBracketId);

            DetectBracketPatterns(change.EntityId, winRateLow, winRateHigh, patterns);
            DetectPerceptionDivergence(change.EntityId, winRateLow, winRateHigh, context, patterns);
            DetectPlanConflicts(change, winRateLow, winRateHigh, patterns);
            DetectPlaytestMismatch(change.EntityId, playtestDelta, patterns);
            DetectIdentityLeverConflict(change, patterns);
            DetectSecondOrderMeta(change, patterns);

            AddAffectedCohorts(change, winRateLow, winRateHigh, pickRateLow, affected);

            dataCommunityScores.Add(ScoreDataVsCommunity(change.EntityId, winRateLow, winRateHigh, context));
            playtestLiveScores.Add(ScorePlaytestVsLive(playtestDelta));
        }

        return new ImpactResult
        {
            WhoIsAffected = affected,
            Alignment = new AlignmentAssessment
            {
                DataVsCommunity = AggregateAlignment(dataCommunityScores),
                PlaytestVsLive = AggregateAlignment(playtestLiveScores),
                Patterns = patterns,
            },
        };
    }

    private static void DetectBracketPatterns(
        string entityId,
        double? winRateLow,
        double? winRateHigh,
        ICollection<ImpactPattern> patterns)
    {
        if (winRateLow is null || winRateHigh is null)
        {
            return;
        }

        if (winRateLow.Value > winRateHigh.Value + BracketSplitThreshold)
        {
            patterns.Add(new ImpactPattern
            {
                Id = "bracket_split_easy_low",
                EntityId = entityId,
                Confidence = "high",
                Evidence =
                [
                    $"wr_low: {winRateLow.Value:F2}",
                    $"wr_high: {winRateHigh.Value:F2}",
                ],
            });
        }

        if (winRateHigh.Value > winRateLow.Value + BracketSplitThreshold)
        {
            patterns.Add(new ImpactPattern
            {
                Id = "bracket_split_skill_ceiling",
                EntityId = entityId,
                Confidence = "high",
                Evidence =
                [
                    $"wr_low: {winRateLow.Value:F2}",
                    $"wr_high: {winRateHigh.Value:F2}",
                ],
            });
        }
    }

    private static void DetectPerceptionDivergence(
        string entityId,
        double? winRateLow,
        double? winRateHigh,
        ContextBundle context,
        ICollection<ImpactPattern> patterns)
    {
        IReadOnlyList<CommunityCluster> clusters = context.Community.Clusters
            .Where(cluster => cluster.EntityId == entityId)
            .ToList();

        if (clusters.Count == 0)
        {
            return;
        }

        bool feelsWeak = clusters.Any(cluster =>
            cluster.Theme.Contains("weak", StringComparison.OrdinalIgnoreCase) ||
            cluster.Sentiment?.Equals("negative", StringComparison.OrdinalIgnoreCase) == true);

        if (!feelsWeak)
        {
            return;
        }

        double? representativeWinRate = winRateLow ?? winRateHigh;
        bool nearAverage = representativeWinRate is >= NearAverageMin and <= NearAverageMax;
        bool dataShowsStrong = winRateLow > StrongWinRate || winRateHigh > StrongWinRate;

        if (!nearAverage && !dataShowsStrong)
        {
            return;
        }

        int volume = clusters.Sum(cluster => cluster.Volume);
        var evidence = new List<string> { $"community_feels_weak: {volume} mentions" };
        if (representativeWinRate.HasValue)
        {
            evidence.Add(nearAverage ? "wr_near_average" : $"wr_strong: {representativeWinRate.Value:F2}");
        }

        patterns.Add(new ImpactPattern
        {
            Id = "perception_vs_data_divergence",
            EntityId = entityId,
            Confidence = dataShowsStrong ? "high" : "medium",
            Evidence = evidence,
        });
    }

    private static void DetectPlanConflicts(
        JoinedChange change,
        double? winRateLow,
        double? winRateHigh,
        ICollection<ImpactPattern> patterns)
    {
        bool isNerf = IsDecrease(change);
        bool isBuff = IsIncrease(change);
        if (!isNerf && !isBuff)
        {
            return;
        }

        if (isNerf && winRateLow > StrongWinRate)
        {
            patterns.Add(new ImpactPattern
            {
                Id = "plan_conflicts_with_data",
                EntityId = change.EntityId,
                Confidence = "high",
                Evidence =
                [
                    "planned_change: nerf",
                    $"wr_low: {winRateLow.Value:F2}",
                    $"field: {change.Field}",
                ],
            });
        }

        if (isBuff && winRateHigh < WeakWinRate)
        {
            patterns.Add(new ImpactPattern
            {
                Id = "plan_conflicts_with_data",
                EntityId = change.EntityId,
                Confidence = "medium",
                Evidence =
                [
                    "planned_change: buff",
                    $"wr_high: {winRateHigh.Value:F2}",
                    $"field: {change.Field}",
                ],
            });
        }
    }

    private static void DetectPlaytestMismatch(
        string entityId,
        double? playtestDelta,
        ICollection<ImpactPattern> patterns)
    {
        if (playtestDelta is null || Math.Abs(playtestDelta.Value) < PlaytestMismatchThreshold)
        {
            return;
        }

        patterns.Add(new ImpactPattern
        {
            Id = "playtest_live_mismatch",
            EntityId = entityId,
            Confidence = Math.Abs(playtestDelta.Value) > 0.15 ? "high" : "medium",
            Evidence = [$"offline_minus_online_win_rate: {playtestDelta.Value:F2}"],
        });
    }

    private static void DetectIdentityLeverConflict(JoinedChange change, ICollection<ImpactPattern> patterns)
    {
        if (!change.LeverStatus.Equals("locked", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        patterns.Add(new ImpactPattern
        {
            Id = "identity_lever_conflict",
            EntityId = change.EntityId,
            Confidence = "high",
            Evidence =
            [
                $"locked_field: {change.Field}",
                $"planned_change: {change.From} -> {change.To}",
            ],
        });
    }

    private static void DetectSecondOrderMeta(JoinedChange change, ICollection<ImpactPattern> patterns)
    {
        bool isTank = change.Role?.Equals("tank", StringComparison.OrdinalIgnoreCase) == true;
        bool damagesNerf = IsDecrease(change) &&
            change.Field.Contains("damage", StringComparison.OrdinalIgnoreCase);

        if (!isTank || !damagesNerf)
        {
            return;
        }

        patterns.Add(new ImpactPattern
        {
            Id = "second_order_meta_risk",
            EntityId = change.EntityId,
            Confidence = "medium",
            Evidence =
            [
                $"role: {change.Role}",
                $"field: {change.Field}",
                $"planned_change: {change.From} -> {change.To}",
            ],
        });
    }

    private static void AddAffectedCohorts(
        JoinedChange change,
        double? winRateLow,
        double? winRateHigh,
        double? pickRateLow,
        ICollection<AffectedCohort> affected)
    {
        if (winRateLow.HasValue)
        {
            affected.Add(new AffectedCohort
            {
                EntityId = change.EntityId,
                EntityName = change.EntityName,
                Cohort = "low_bracket",
                Impact = winRateLow.Value > StrongWinRate ? "high" : "medium",
                Reason = BuildReason(change, winRateLow.Value, pickRateLow, "low bracket"),
            });
        }

        if (winRateHigh.HasValue)
        {
            affected.Add(new AffectedCohort
            {
                EntityId = change.EntityId,
                EntityName = change.EntityName,
                Cohort = "high_bracket",
                Impact = winRateHigh.Value < WeakWinRate ? "high" : "medium",
                Reason = BuildReason(change, winRateHigh.Value, null, "high bracket"),
            });
        }
    }

    private static string BuildReason(
        JoinedChange change,
        double winRate,
        double? pickRate,
        string cohortLabel)
    {
        string changeSummary = $"{change.Field} {change.From} -> {change.To}";
        if (!string.IsNullOrWhiteSpace(change.Delta))
        {
            changeSummary += $" ({change.Delta})";
        }

        string pickSummary = pickRate.HasValue ? $"; pick_rate: {pickRate.Value:F2}" : string.Empty;
        return $"{winRate:P0} WR in {cohortLabel}; patch changes {changeSummary}{pickSummary}";
    }

    private static int ScoreDataVsCommunity(
        string entityId,
        double? winRateLow,
        double? winRateHigh,
        ContextBundle context)
    {
        bool hasNegativeCommunity = context.Community.Clusters.Any(cluster =>
            cluster.EntityId == entityId &&
            (cluster.Theme.Contains("weak", StringComparison.OrdinalIgnoreCase) ||
             cluster.Sentiment?.Equals("negative", StringComparison.OrdinalIgnoreCase) == true));

        if (!hasNegativeCommunity)
        {
            return 0;
        }

        double? representative = winRateLow ?? winRateHigh;
        if (representative is >= NearAverageMin and <= NearAverageMax)
        {
            return 1;
        }

        if (winRateLow > StrongWinRate || winRateHigh > StrongWinRate)
        {
            return 1;
        }

        return 0;
    }

    private static int ScorePlaytestVsLive(double? playtestDelta) =>
        playtestDelta is not null && Math.Abs(playtestDelta.Value) >= PlaytestMismatchThreshold ? 1 : 0;

    private static string AggregateAlignment(IReadOnlyList<int> scores)
    {
        if (scores.Count == 0)
        {
            return "unknown";
        }

        int divergent = scores.Count(score => score > 0);
        if (divergent == 0)
        {
            return "aligned";
        }

        if (divergent == scores.Count)
        {
            return "divergent";
        }

        return "mixed";
    }

    private static double? ReadOnlineWinRate(
        IReadOnlyDictionary<string, CohortMetrics> cohorts,
        string? bracketId) =>
        bracketId is not null &&
        cohorts.TryGetValue(bracketId, out CohortMetrics? cohort) &&
        cohort.Sources.TryGetValue("online", out SourceMetrics? online)
            ? online.WinRate
            : null;

    private static double? ReadOnlinePickRate(
        IReadOnlyDictionary<string, CohortMetrics> cohorts,
        string? bracketId) =>
        bracketId is not null &&
        cohorts.TryGetValue(bracketId, out CohortMetrics? cohort) &&
        cohort.Sources.TryGetValue("online", out SourceMetrics? online)
            ? online.PickRate
            : null;

    private static double? ReadPlaytestDelta(
        IReadOnlyDictionary<string, CohortMetrics> cohorts,
        string? bracketId) =>
        bracketId is not null &&
        cohorts.TryGetValue(bracketId, out CohortMetrics? cohort)
            ? cohort.Comparison?.WinRateDelta
            : null;

    private static BracketPair ResolveBrackets(IReadOnlyList<BracketSnapshot> brackets)
    {
        if (brackets.Count == 0)
        {
            return new BracketPair(null, null);
        }

        if (brackets.Count == 1)
        {
            return new BracketPair(brackets[0].Id, brackets[0].Id);
        }

        return new BracketPair(brackets[0].Id, brackets[^1].Id);
    }

    private static bool IsDecrease(JoinedChange change)
    {
        if (change.From is null || change.To is null)
        {
            return change.Delta?.StartsWith('-') == true;
        }

        if (TryToDouble(change.From, out double from) && TryToDouble(change.To, out double to))
        {
            return to < from;
        }

        return false;
    }

    private static bool IsIncrease(JoinedChange change)
    {
        if (change.From is null || change.To is null)
        {
            return change.Delta?.StartsWith('+') == true;
        }

        if (TryToDouble(change.From, out double from) && TryToDouble(change.To, out double to))
        {
            return to > from;
        }

        return false;
    }

    private static bool TryToDouble(object? value, out double number)
    {
        switch (value)
        {
            case double d:
                number = d;
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case float f:
                number = f;
                return true;
            case decimal m:
                number = (double)m;
                return true;
            case string s when double.TryParse(s, out number):
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number &&
                                          element.TryGetDouble(out number):
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.String &&
                                          double.TryParse(element.GetString(), out number):
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private sealed record BracketPair(string? LowBracketId, string? HighBracketId);
}
