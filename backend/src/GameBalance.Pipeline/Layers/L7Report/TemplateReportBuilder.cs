using System.Text;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;

namespace GameBalance.Pipeline.Layers.L7Report;

/// <summary>Deterministic report builder used when LLM is unavailable.</summary>
public static class TemplateReportBuilder
{
    public static InsightReport Build(
        ImpactResult impact,
        RiskResult risks,
        ContextBundle context)
    {
        string version = context.UpdatePlan.Version ?? "unversioned";
        string proposedChanges = string.Join(
            "; ",
            context.JoinedChanges.Select(change =>
                $"{change.EntityName} {change.Field} {change.From} -> {change.To}"));

        string executiveSummary = BuildExecutiveSummary(impact, risks, proposedChanges);
        string markdown = BuildMarkdown(version, executiveSummary, impact, risks, context, proposedChanges);
        string draftComms = BuildDraftComms(impact, risks, context);

        return new InsightReport
        {
            ExecutiveSummary = executiveSummary,
            WhoIsAffected = impact.WhoIsAffected,
            Alignment = impact.Alignment,
            Risks = risks.Risks,
            SolutionPaths = risks.SolutionPaths,
            ValidationPlan = risks.ValidationPlan,
            ReportMarkdown = markdown,
            DraftPlayerComms = draftComms,
            GeneratedBy = "template",
        };
    }

    private static string BuildExecutiveSummary(
        ImpactResult impact,
        RiskResult risks,
        string proposedChanges)
    {
        var builder = new StringBuilder();
        builder.Append($"Planned changes: {proposedChanges}. ");
        builder.Append(
            $"Data vs community is {impact.Alignment.DataVsCommunity}; playtest vs live is {impact.Alignment.PlaytestVsLive}. ");

        if (impact.Alignment.Patterns.Count > 0)
        {
            builder.Append("Key patterns: ");
            builder.Append(string.Join(
                ", ",
                impact.Alignment.Patterns.Select(pattern => $"{pattern.Id} ({pattern.EntityId})")));
            builder.Append(". ");
        }

        if (risks.Risks.Count > 0)
        {
            RiskItem topRisk = risks.Risks
                .OrderByDescending(risk => risk.Severity switch
                {
                    "high" => 3,
                    "medium" => 2,
                    _ => 1,
                })
                .First();
            builder.Append($"Top risk: {topRisk.Title} ({topRisk.Severity}). ");
        }

        builder.Append("Designer decides final patch direction.");
        return builder.ToString().Trim();
    }

    private static string BuildMarkdown(
        string version,
        string executiveSummary,
        ImpactResult impact,
        RiskResult risks,
        ContextBundle context,
        string proposedChanges)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Patch Brief — v{version}");
        builder.AppendLine();
        builder.AppendLine("## Executive Summary");
        builder.AppendLine(executiveSummary);
        builder.AppendLine();
        builder.AppendLine("## Proposed Changes");
        builder.AppendLine(proposedChanges);
        builder.AppendLine();
        builder.AppendLine("## Who Is Affected");

        if (impact.WhoIsAffected.Count == 0)
        {
            builder.AppendLine("- No affected cohorts identified from current metrics.");
        }
        else
        {
            foreach (AffectedCohort cohort in impact.WhoIsAffected)
            {
                builder.AppendLine(
                    $"- **{cohort.EntityName}** ({cohort.Cohort}, {cohort.Impact} impact): {cohort.Reason}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Alignment");
        builder.AppendLine($"- Data vs community: **{impact.Alignment.DataVsCommunity}**");
        builder.AppendLine($"- Playtest vs live: **{impact.Alignment.PlaytestVsLive}**");

        if (impact.Alignment.Patterns.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("### Patterns");
            foreach (ImpactPattern pattern in impact.Alignment.Patterns)
            {
                builder.AppendLine(
                    $"- `{pattern.Id}` on **{pattern.EntityId}** ({pattern.Confidence}): {string.Join(", ", pattern.Evidence)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Risks");
        if (risks.Risks.Count == 0)
        {
            builder.AppendLine("- No major risks flagged.");
        }
        else
        {
            int index = 1;
            foreach (RiskItem risk in risks.Risks)
            {
                builder.AppendLine(
                    $"{index}. **{risk.Title}** (`{risk.Id}`, {risk.Severity}) — {string.Join("; ", risk.Evidence)}");
                index++;
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Suggested Paths (designer decides)");
        if (risks.SolutionPaths.Count == 0)
        {
            builder.AppendLine("- No solution paths generated.");
        }
        else
        {
            int index = 1;
            foreach (SolutionPath path in risks.SolutionPaths)
            {
                builder.AppendLine($"{index}. **{path.Type}** ({path.Confidence}): {path.Rationale}");
                index++;
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Validation Plan");
        foreach (string step in risks.ValidationPlan)
        {
            builder.AppendLine($"- {step}");
        }

        if (context.Community.Clusters.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Community Signals");
            foreach (CommunityCluster cluster in context.Community.Clusters)
            {
                builder.AppendLine(
                    $"- {cluster.EntityId}: {cluster.Theme} ({cluster.Volume} mentions, {cluster.Sentiment ?? "neutral"})");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildDraftComms(
        ImpactResult impact,
        RiskResult risks,
        ContextBundle context)
    {
        string entityNames = string.Join(
            ", ",
            context.JoinedChanges.Select(change => change.EntityName).Distinct());

        if (impact.Alignment.DataVsCommunity.Equals("divergent", StringComparison.OrdinalIgnoreCase))
        {
            return
                $"We've seen feedback on {entityNames}. Our data shows mixed performance across skill brackets; " +
                "we're reviewing game feel and clarity before making balance changes.";
        }

        if (risks.Risks.Any(risk => risk.Id == "plan_conflicts_with_data"))
        {
            return
                $"We're evaluating planned updates for {entityNames} against live and playtest data. " +
                "We'll share more once validation is complete.";
        }

        return
            $"We're monitoring {entityNames} performance and community feedback as we prepare the next patch.";
    }
}
