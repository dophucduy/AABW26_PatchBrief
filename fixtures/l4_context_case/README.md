# L4 canonical context fixture

This directory is an isolated, canonical example for Layer 4. It does not
replace or restore files currently being edited in `fixtures/demo_case`.

Inputs:

- `game_definition.json` — entities, roles, stats, brackets.
- `rules.json` — locked/open levers keyed by entity ID.
- `update_plan.json` — proposed changes keyed by `target` entity ID.
- `community.json` — normalized community clusters.
- `metrics.json` — optional L3 output used for telemetry ID validation.

Output:

- `context_bundle.json` — expected canonical L4 output for L5-L7.

Validation rules:

1. Rule and update-plan entity IDs must exist in `game_definition.entities`.
2. Metric/community IDs that cannot be resolved are reported as warnings.
3. Each planned change is joined with entity name, role, and lever status.
4. `stats.` prefixes in update-plan fields are normalized before rule matching.
