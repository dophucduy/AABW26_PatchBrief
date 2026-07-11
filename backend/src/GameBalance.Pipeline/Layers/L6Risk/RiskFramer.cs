using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L5Impact;

namespace GameBalance.Pipeline.Layers.L6Risk;

/// <summary>L6 - maps impact patterns to risks, solution paths, and validation steps.</summary>
public sealed class RiskFramer
{
    public RiskResult Analyze(ImpactResult impact, ContextBundle context)
    {
        var risks = new List<RiskItem>();
        var solutions = new List<SolutionPath>();
        var validation = new HashSet<string>(StringComparer.Ordinal);

        foreach (ImpactPattern pattern in impact.Alignment.Patterns)
        {
            switch (pattern.Id)
            {
                case "perception_vs_data_divergence":
                    AddRisk(risks, "stakeholder_conflict", "high",
                        "Community wants buff; data does not support it", pattern.Evidence);
                    AddRisk(risks, "comms_backlash", "medium",
                        "High community volume may amplify controversial change", pattern.Evidence);
                    AddSolution(solutions, "comms_only", "medium",
                        "Address perception before numeric change", pattern.Id, context);
                    AddSolution(solutions, "kit_redesign", "low",
                        "If playtests confirm 'unfun' not 'weak', review kit feel", pattern.Id, context);
                    validation.Add("Survey: fun to play vs feels weak");
                    break;

                case "bracket_split_easy_low":
                    AddRisk(risks, "stakeholder_conflict", "medium",
                        "Strong in low bracket; global change may overcorrect", pattern.Evidence);
                    AddSolution(solutions, "targeted_by_bracket", "medium",
                        "If change needed, target low-bracket abuse only", pattern.Id, context);
                    validation.Add("Playtest entity in low vs high bracket");
                    break;

                case "bracket_split_skill_ceiling":
                    AddRisk(risks, "second_order_meta", "medium",
                        "High-skill strength may shift competitive meta", pattern.Evidence);
                    AddSolution(solutions, "targeted_by_bracket", "medium",
                        "Consider high-bracket targeted tuning", pattern.Id, context);
                    validation.Add("Monitor high-bracket pick/ban rates");
                    break;

                case "plan_conflicts_with_data":
                    AddRisk(risks, "stakeholder_conflict", "high",
                        "Planned change conflicts with current performance data", pattern.Evidence);
                    AddSolution(solutions, "iterate_playtest", "medium",
                        "Validate change direction with focused playtest", pattern.Id, context);
                    AddSolution(solutions, "communication_framing", "medium",
                        "Explain rationale if shipping despite data conflict", pattern.Id, context);
                    validation.Add("Re-run playtest with proposed numbers");
                    break;

                case "playtest_live_mismatch":
                    AddRisk(risks, "symptom_not_root", "medium",
                        "Playtest data may not represent live population", pattern.Evidence);
                    AddSolution(solutions, "iterate_playtest", "high",
                        "Collect more live-like playtest data before shipping", pattern.Id, context);
                    validation.Add("Compare playtest cohort to live bracket mix");
                    break;

                case "identity_lever_conflict":
                    AddRisk(risks, "identity_lever_conflict", "high",
                        "Update plan modifies a locked identity lever", pattern.Evidence);
                    AddSolution(solutions, "tune_numbers", "low",
                        "Blocked: lever is locked by rules", pattern.Id, context, blocked: true);
                    AddSolution(solutions, "solve_elsewhere", "medium",
                        "Address goal through open levers or kit clarity", pattern.Id, context);
                    validation.Add("Review rules.json locked levers with design lead");
                    break;

                case "second_order_meta_risk":
                    AddRisk(risks, "second_order_meta", "medium",
                        "Tank damage nerf may reduce frontline presence", pattern.Evidence);
                    AddSolution(solutions, "iterate_playtest", "medium",
                        "Test team comp diversity after proposed change", pattern.Id, context);
                    validation.Add("Monitor pick rate 1 week if micro-patch ships");
                    break;
            }
        }

        foreach (JoinedChange change in context.JoinedChanges)
        {
            if (change.LeverStatus.Equals("locked", StringComparison.OrdinalIgnoreCase) &&
                !impact.Alignment.Patterns.Any(pattern =>
                    pattern.EntityId == change.EntityId && pattern.Id == "identity_lever_conflict"))
            {
                AddRisk(risks, "identity_lever_conflict", "high",
                    $"Locked lever '{change.Field}' targeted by update plan",
                    [$"entity: {change.EntityName}", $"field: {change.Field}"]);
            }
        }

        if (validation.Count == 0)
        {
            validation.Add("Review metrics after patch on affected entities");
        }

        return new RiskResult
        {
            Risks = DeduplicateRisks(risks),
            SolutionPaths = DeduplicateSolutions(solutions),
            ValidationPlan = validation.ToList(),
        };
    }

    private static void AddRisk(
        ICollection<RiskItem> risks,
        string id,
        string severity,
        string title,
        IReadOnlyList<string> evidence)
    {
        risks.Add(new RiskItem
        {
            Id = id,
            Severity = severity,
            Title = title,
            Evidence = evidence.ToList(),
        });
    }

    private static void AddSolution(
        ICollection<SolutionPath> solutions,
        string type,
        string confidence,
        string rationale,
        string patternId,
        ContextBundle context,
        bool blocked = false)
    {
        if (blocked)
        {
            solutions.Add(new SolutionPath
            {
                Type = type,
                Confidence = "low",
                Rationale = rationale,
                DesignerDecides = true,
            });
            return;
        }

        bool lockedLeverInvolved = context.JoinedChanges.Any(change =>
            change.LeverStatus.Equals("locked", StringComparison.OrdinalIgnoreCase));

        if (lockedLeverInvolved && type.Equals("tune_numbers", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        solutions.Add(new SolutionPath
        {
            Type = type,
            Confidence = confidence,
            Rationale = $"{rationale} (pattern: {patternId})",
            DesignerDecides = true,
        });
    }

    private static IReadOnlyList<RiskItem> DeduplicateRisks(IReadOnlyList<RiskItem> risks) =>
        risks
            .GroupBy(risk => $"{risk.Id}|{risk.Title}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

    private static IReadOnlyList<SolutionPath> DeduplicateSolutions(IReadOnlyList<SolutionPath> solutions) =>
        solutions
            .GroupBy(path => $"{path.Type}|{path.Rationale}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
}
