using System.Globalization;
using System.Text.Json.Serialization;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;

namespace GameBalance.Api;

internal static class PatchReportMapper
{
    public static PatchReportResponse ToPatchReport(AnalyzeResponse response)
    {
        InsightReport report = response.Report
            ?? throw new InvalidOperationException("Pipeline did not produce a report");
        ContextBundle? context = response.Context;
        ImpactResult? impact = response.Impact;
        RiskResult? risk = response.Risk;
        MetricResult? metric = response.Metric;

        string reportId = $"rpt_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        bool llmUsed = string.Equals(report.GeneratedBy, "llm", StringComparison.OrdinalIgnoreCase);

        return new PatchReportResponse
        {
            ReportId = reportId,
            GeneratedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            LlmUsed = llmUsed,
            ReportMode = "live",
            Overview = BuildOverview(impact, risk, context),
            ExecutiveSummary = report.ExecutiveSummary,
            WhoIsAffected = MapAffected(report.WhoIsAffected, context, metric),
            ProposedChanges = MapProposedChanges(context),
            Alignment = MapAlignment(report.Alignment),
            Risks = MapRisks(report.Risks),
            SolutionPaths = MapSolutionPaths(report.SolutionPaths),
            ValidationPlan = report.ValidationPlan.ToList(),
            DraftPlayerComms = report.DraftPlayerComms ?? string.Empty,
            ReportMarkdown = report.ReportMarkdown,
        };
    }

    private static ReportOverviewResponse? BuildOverview(
        ImpactResult? impact,
        RiskResult? risk,
        ContextBundle? context)
    {
        if (impact is null || risk is null)
        {
            return null;
        }

        int communityMentions = context?.Community.Clusters.Sum(cluster => cluster.Volume) ?? 0;
        string overallRisk = risk.Risks
            .OrderByDescending(item => SeverityRank(item.Severity))
            .Select(item => item.Severity)
            .FirstOrDefault() ?? "low";

        return new ReportOverviewResponse
        {
            AffectedSessionsPercent = EstimateAffectedSessionsPercent(impact),
            CommunityMentions = communityMentions,
            OverallRisk = overallRisk,
        };
    }

    private static int EstimateAffectedSessionsPercent(ImpactResult impact)
    {
        int highImpact = impact.WhoIsAffected.Count(item =>
            string.Equals(item.Impact, "high", StringComparison.OrdinalIgnoreCase));
        if (highImpact == 0)
        {
            return 0;
        }

        return Math.Min(100, highImpact * 18);
    }

    private static int SeverityRank(string severity) => severity.ToLowerInvariant() switch
    {
        "high" => 3,
        "medium" => 2,
        _ => 1,
    };

    private static List<AffectedEntityResponse> MapAffected(
        IReadOnlyList<AffectedCohort> cohorts,
        ContextBundle? context,
        MetricResult? metric)
    {
        Dictionary<string, string> roles = context?.JoinedChanges
            .GroupBy(change => change.EntityId)
            .ToDictionary(group => group.Key, group => group.First().Role ?? "unknown")
            ?? new Dictionary<string, string>();

        return cohorts.Select(cohort =>
        {
            CohortMetricsResponse? metrics = TryReadMetrics(metric, cohort.EntityId, cohort.Cohort);
            return new AffectedEntityResponse
            {
                EntityId = cohort.EntityId,
                EntityName = cohort.EntityName,
                Role = roles.GetValueOrDefault(cohort.EntityId, "unknown"),
                Cohort = cohort.Cohort,
                Impact = cohort.Impact,
                Metrics = metrics,
            };
        }).ToList();
    }

    private static CohortMetricsResponse? TryReadMetrics(
        MetricResult? metric,
        string entityId,
        string cohortLabel)
    {
        if (metric is null || !metric.Metrics.TryGetValue(entityId, out IReadOnlyDictionary<string, CohortMetrics>? brackets))
        {
            return null;
        }

        CohortMetrics? match = brackets.Values.FirstOrDefault(item =>
            string.Equals(item.BracketId, cohortLabel, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.BracketId, cohortLabel.Replace(' ', '_'), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            match = brackets.Values.FirstOrDefault();
        }

        if (match is null)
        {
            return null;
        }

        SourceMetrics? online = match.Sources.GetValueOrDefault("online");
        return new CohortMetricsResponse
        {
            PickRate = online?.PickRate is double pickRate ? Math.Round(pickRate * 100, 1) : null,
            WinRate = online?.WinRate is double winRate ? Math.Round(winRate * 100, 1) : null,
        };
    }

    private static List<ProposedChangeResponse> MapProposedChanges(ContextBundle? context) =>
        context?.JoinedChanges.Select(change => new ProposedChangeResponse
        {
            Target = change.EntityId,
            EntityName = change.EntityName,
            Field = change.Field,
            From = change.From ?? string.Empty,
            To = change.To ?? string.Empty,
            Delta = change.Delta ?? string.Empty,
            Role = change.Role ?? "unknown",
        }).ToList() ?? [];

    private static AlignmentResponse MapAlignment(AlignmentAssessment alignment) => new()
    {
        DataVsCommunity = alignment.DataVsCommunity,
        PlaytestVsLive = alignment.PlaytestVsLive,
        Patterns = alignment.Patterns.Select(pattern => new AlignmentPatternResponse
        {
            Id = pattern.Id,
            Title = HumanizeId(pattern.Id),
            Description = pattern.Evidence.FirstOrDefault() ?? pattern.Id,
            Confidence = pattern.Confidence,
        }).ToList(),
    };

    private static List<RiskResponse> MapRisks(IReadOnlyList<RiskItem> risks) =>
        risks.Select(risk => new RiskResponse
        {
            Id = risk.Id,
            Severity = risk.Severity,
            Title = risk.Title,
            Evidence = risk.Evidence.ToList(),
        }).ToList();

    private static List<SolutionPathResponse> MapSolutionPaths(IReadOnlyList<SolutionPath> paths) =>
        paths.Select(path => new SolutionPathResponse
        {
            Type = path.Type,
            Label = HumanizeId(path.Type),
            Confidence = path.Confidence,
            Rationale = path.Rationale,
            DesignerDecides = path.DesignerDecides,
        }).ToList();

    private static string HumanizeId(string value) =>
        string.Join(' ', value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
}

internal sealed class PatchReportResponse
{
    [JsonPropertyName("report_id")]
    public required string ReportId { get; init; }

    [JsonPropertyName("generated_at")]
    public required string GeneratedAt { get; init; }

    [JsonPropertyName("llm_used")]
    public required bool LlmUsed { get; init; }

    [JsonPropertyName("report_mode")]
    public string? ReportMode { get; init; }

    [JsonPropertyName("overview")]
    public ReportOverviewResponse? Overview { get; init; }

    [JsonPropertyName("executive_summary")]
    public required string ExecutiveSummary { get; init; }

    [JsonPropertyName("who_is_affected")]
    public required List<AffectedEntityResponse> WhoIsAffected { get; init; }

    [JsonPropertyName("proposed_changes")]
    public required List<ProposedChangeResponse> ProposedChanges { get; init; }

    [JsonPropertyName("alignment")]
    public required AlignmentResponse Alignment { get; init; }

    [JsonPropertyName("risks")]
    public required List<RiskResponse> Risks { get; init; }

    [JsonPropertyName("solution_paths")]
    public required List<SolutionPathResponse> SolutionPaths { get; init; }

    [JsonPropertyName("validation_plan")]
    public required List<string> ValidationPlan { get; init; }

    [JsonPropertyName("draft_player_comms")]
    public required string DraftPlayerComms { get; init; }

    [JsonPropertyName("report_markdown")]
    public string? ReportMarkdown { get; init; }
}

internal sealed class ReportOverviewResponse
{
    [JsonPropertyName("affected_sessions_percent")]
    public int AffectedSessionsPercent { get; init; }

    [JsonPropertyName("community_mentions")]
    public int CommunityMentions { get; init; }

    [JsonPropertyName("overall_risk")]
    public required string OverallRisk { get; init; }
}

internal sealed class AffectedEntityResponse
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("entity_name")]
    public required string EntityName { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("cohort")]
    public required string Cohort { get; init; }

    [JsonPropertyName("impact")]
    public required string Impact { get; init; }

    [JsonPropertyName("metrics")]
    public CohortMetricsResponse? Metrics { get; init; }
}

internal sealed class CohortMetricsResponse
{
    [JsonPropertyName("pick_rate")]
    public double? PickRate { get; init; }

    [JsonPropertyName("win_rate")]
    public double? WinRate { get; init; }
}

internal sealed class ProposedChangeResponse
{
    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("entity_name")]
    public required string EntityName { get; init; }

    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("from")]
    public required object From { get; init; }

    [JsonPropertyName("to")]
    public required object To { get; init; }

    [JsonPropertyName("delta")]
    public required string Delta { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }
}

internal sealed class AlignmentResponse
{
    [JsonPropertyName("data_vs_community")]
    public required string DataVsCommunity { get; init; }

    [JsonPropertyName("playtest_vs_live")]
    public required string PlaytestVsLive { get; init; }

    [JsonPropertyName("patterns")]
    public required List<AlignmentPatternResponse> Patterns { get; init; }
}

internal sealed class AlignmentPatternResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }
}

internal sealed class RiskResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("evidence")]
    public required List<string> Evidence { get; init; }
}

internal sealed class SolutionPathResponse
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("confidence")]
    public required string Confidence { get; init; }

    [JsonPropertyName("rationale")]
    public required string Rationale { get; init; }

    [JsonPropertyName("designer_decides")]
    public bool DesignerDecides { get; init; } = true;
}
