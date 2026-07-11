# Pipeline Layers — Explanation & Examples

**Product:** Patch Brief / Balance Decision Tool  
**Purpose:** Help game designers decide on balance patches by combining player data, playtest data, game definition, rules, community sentiment, and update plans — **before shipping**.

**Key principle:** Layers 0–6 are **deterministic code** (C#). Only Layer 7 uses **LLM** to write the final report. AI does not invent numbers.

---

## Architecture Overview

```
                    ┌─────────────────────────────────────────┐
                    │              USER UPLOADS               │
                    │  player logs · playtest · game def      │
                    │  rules · update plan · community        │
                    └─────────────────┬───────────────────────┘
                                      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ WEB (Backend — C#)                                                      │
│                                                                         │
│  L0 Adapter → L1 Ingest → L2 Semantic → L3 Metric → L4 Context         │
└─────────────────────────────────────────────────────────────────────────┘
                                      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ AI                                                                      │
│                                                                         │
│  L5 Impact & Alignment → L6 Risk & Solutions → L7 Report (LLM)         │
└─────────────────────────────────────────────────────────────────────────┘
                                      ▼
                              Report to User
```

---

## Layer 0 — Adapter (Mapping)

### What it does

Maps each studio's **different JSON format** into our **canonical schema** (one standard language for the pipeline).

- AI **suggests** mapping once during setup
- User **fixes** wrong fields
- Saved as `adapter.json`
- Every future analyze run uses saved mapping — **no AI in this step at runtime**

### What it does NOT do

- Does not analyze balance
- Does not run on every upload without a saved adapter (after setup)

### Example

**Studio raw log:**

```json
{
  "type": "Death",
  "characterId": "char_A",
  "gameTime": 45.1,
  "playerRank": "bronze"
}
```

**After adapter (`adapter.json`):**

```json
{
  "type": "death",
  "entity_id": "char_A",
  "t": 45.1,
  "bracket": "bronze",
  "source": "live"
}
```

**Mapping config:**

```json
{
  "field_map": {
    "characterId": "entity_id",
    "gameTime": "t",
    "playerRank": "bracket"
  },
  "event_map": {
    "Death": "death"
  }
}
```

---

## Layer 1 — Ingest & Normalize

### What it does

- Receives canonical events (after Layer 0)
- Validates against `schema.json` (required fields, types)
- Sorts by time, deduplicates
- Tags each record: `source: live | playtest`, `build_id`, `session_id`
- Splits online vs offline streams

### What it does NOT do

- Does not interpret gameplay meaning
- Does not calculate win rates

### Example

**Input:** Mixed upload files (online + offline)

**Output:** Clean event stream

```json
[
  { "session_id": "s001", "source": "live",   "type": "entity_pick", "entity_id": "char_A", "t": 0 },
  { "session_id": "s001", "source": "live",   "type": "death",       "entity_id": "char_A", "t": 45.1, "bracket": "bronze" },
  { "session_id": "s002", "source": "playtest", "type": "match_end", "entity_id": "char_A", "t": 600, "result": "win" }
]
```

**Validation error example:**

```json
{
  "error": "Event at row 12 missing required field: t"
}
```

---

## Layer 2 — Gameplay Semantic

### What it does

Turns raw events into **gameplay concepts** designers understand:

| Concept | Meaning |
|---------|---------|
| `Segment` | Time spent in one area / match phase |
| `Attempt` | Death → retry until success |
| `Loop` | Same segment, many deaths in short time |
| `Progress path` | Order of areas / modes visited |
| `Bracket behavior` | How low vs high skill players use features differently |

### What it does NOT do

- Does not conclude "this is a difficulty spike" (that's Layer 5)
- Does not compare to community or update plan

### Example

**Input:** Events from Layer 1 for session `s001`

**Output:** Session model

```json
{
  "session_id": "s001",
  "source": "live",
  "entity_id": "char_A",
  "bracket": "bronze",
  "segments": [
    {
      "area": "ranked_match",
      "duration_sec": 320,
      "deaths": 2,
      "abilities_used": ["shield", "charge"]
    }
  ],
  "behavior_flags": [
    "uses_full_mag_before_reload"
  ]
}
```

**GMTK Gears of War example (conceptual):**

| Bracket | Behavior flag |
|---------|----------------|
| Low skill | `empties_mag_before_reload` |
| High skill | `uses_active_reload` |

This flag feeds Layer 5 — not labeled "good" or "bad" here, just **observed behavior**.

---

## Layer 3 — Metric & Cohort

### What it does

Aggregates session models across **many players** into numbers:

- Win rate, pick rate, death rate
- By **entity** (character / weapon)
- By **bracket** (low / high skill)
- By **source** (`live` vs `playtest`)

### What it does NOT do

- Does not judge if numbers are good or bad
- Does not read community or update plan

### Example

**Input:** 500 sessions (live) + 50 sessions (playtest)

**Output:** `metrics.json`

```json
{
  "char_A": {
    "live": {
      "sessions": 500,
      "pick_rate_low": 0.22,
      "pick_rate_high": 0.08,
      "win_rate_low": 0.58,
      "win_rate_high": 0.49,
      "death_rate_low": 0.12
    },
    "playtest": {
      "sessions": 50,
      "win_rate_all": 0.51
    }
  },
  "char_B": {
    "live": {
      "win_rate_low": 0.42,
      "win_rate_high": 0.61,
      "pick_rate_low": 0.05,
      "pick_rate_high": 0.14
    }
  }
}
```

**How to read:**

- `char_A` — strong in low bracket (58% WR), average in high (49%)
- `char_B` — weak in low (42%), strong in high (61%) → high skill ceiling character

---

## Layer 4 — Context

### What it does

Merges **design-side data** that metrics alone cannot explain. Four parts:

| Part | File | Answers |
|------|------|---------|
| **Game definition** | `game_definition.json` | What exists? Roster, stats, roles |
| **Rules** | `rules.json` | What can / cannot change? |
| **Update plan** | `update_plan.json` | What will change in this patch? |
| **Community** | `community.json` | What do players think? |

Validates IDs: every `entity_id` in metrics and update plan must exist in game definition.

### What it does NOT do

- Does not compute metrics from logs (that's L3)
- Does not generate risks or solutions (that's L6)

### Example — Game definition

```json
{
  "game_id": "arena_moba",
  "genre": "MOBA",
  "entities": [
    {
      "id": "char_A",
      "name": "Ironclad",
      "role": "tank",
      "stats": { "hp": 1200, "base_damage": 45 },
      "tags": ["beginner_friendly"]
    },
    {
      "id": "char_B",
      "name": "Vex",
      "role": "assassin",
      "stats": { "hp": 800, "base_damage": 85 },
      "tags": ["high_skill_ceiling"]
    }
  ]
}
```

### Example — Rules

```json
{
  "char_A": {
    "locked": ["identity_skill_shield"],
    "open": ["base_damage", "cooldown_q"]
  },
  "char_B": {
    "locked": ["base_damage"],
    "open": ["cooldown_ult", "range"]
  }
}
```

### Example — Update plan

```json
{
  "version": "0.4.3",
  "changes": [
    {
      "target": "char_A",
      "field": "base_damage",
      "from": 45,
      "to": 40,
      "delta": "-11%"
    }
  ]
}
```

### Example — Community

```json
{
  "clusters": [
    {
      "entity_id": "char_A",
      "theme": "feels_weak",
      "volume": 340,
      "sentiment": "negative",
      "sample_quotes": ["Ironclad needs buff", "char A useless in ranked"]
    }
  ]
}
```

### Output — Context bundle

```json
{
  "game_definition": { },
  "rules": { },
  "update_plan": { },
  "community": { },
  "joined_changes": [
    {
      "entity_id": "char_A",
      "entity_name": "Ironclad",
      "role": "tank",
      "field": "base_damage",
      "from": 45,
      "to": 40,
      "delta": "-11%",
      "lever_status": "open"
    }
  ]
}
```

---

## Layer 5 — Impact & Alignment

### What it does

Combines **metrics (L3)** + **context (L4)** to answer:

1. **Who is affected?** — which cohorts, which entities
2. **Does data align with community?**
3. **Does playtest match live?**
4. **Does the update plan conflict with data?**

Uses **rule-based patterns** — not LLM.

### What it does NOT do

- Does not auto-apply balance changes
- Does not write the final report prose (that's L7)

### Pattern examples

| Pattern ID | Condition | Meaning |
|------------|-----------|---------|
| `bracket_split_easy_low` | WR low >> WR high | Easy kit, counterable by skilled play |
| `bracket_split_skill_ceiling` | WR high >> WR low | Hard to master, strong in skilled hands |
| `perception_vs_data_divergence` | Community negative + WR near average | Kit/feel problem, not raw numbers |
| `plan_conflicts_with_data` | Plan buffs entity already high WR | Overbuff risk |
| `playtest_live_mismatch` | Playtest WR ≠ live WR | Playtest not representative |

### Example output

```json
{
  "who_is_affected": [
    {
      "entity_id": "char_A",
      "entity_name": "Ironclad",
      "cohort": "low_bracket",
      "impact": "high",
      "reason": "58% WR low bracket; patch reduces damage further"
    }
  ],
  "alignment": {
    "data_vs_community": "divergent",
    "playtest_vs_live": "aligned",
    "patterns": [
      {
        "id": "bracket_split_easy_low",
        "entity_id": "char_A",
        "confidence": "high",
        "evidence": ["wr_low: 0.58", "wr_high: 0.49"]
      },
      {
        "id": "perception_vs_data_divergence",
        "entity_id": "char_A",
        "confidence": "medium",
        "evidence": ["wr_near_average", "community_feels_weak: 340"]
      }
    ]
  }
}
```

**Note:** Community says char_A is weak, but data shows 58% WR in low bracket → **divergent** → likely perception or kit feel, not raw underpowered stats.

---

## Layer 6 — Risk & Solution Framing

### What it does

From Layer 5 insights, produces:

1. **Risks** — what could go wrong if patch ships
2. **Solution paths** — types of fix (not prescriptions)
3. **Validation plan** — what to test next

Inspired by GMTK problem-solving: root cause vs symptom, second-order effects, bracket-targeted fixes.

### What it does NOT do

- Does not say "reduce damage by 5%" as a command
- Does not replace designer judgment

### Risk types

| Risk ID | Example |
|---------|---------|
| `stakeholder_conflict` | Casual wants buff; ranked meta already stable |
| `second_order_meta` | Nerf tank → team comp shifts |
| `identity_lever_conflict` | Update plan changes locked stat |
| `symptom_not_root` | "Feels weak" but data OK → wrong fix type |
| `comms_backlash` | High community volume + controversial change |

### Solution path types

| Path | When to suggest |
|------|-----------------|
| `tune_numbers` | Clear numeric lever + data supports change |
| `targeted_by_bracket` | Problem only in one skill bracket (Gears magic bullets pattern) |
| `kit_redesign` | Data OK but community bored / unrewarded |
| `solve_elsewhere` | Fix tutorial, UI, onboarding — not stats |
| `comms_only` | Stakeholder conflict; explain before changing |
| `iterate_playtest` | Live vs playtest mismatch — need more data |

### Example output

```json
{
  "risks": [
    {
      "id": "stakeholder_conflict",
      "severity": "high",
      "title": "Community wants buff; low bracket WR already 58%",
      "evidence": [
        "community theme feels_weak: 340 mentions",
        "wr_low: 0.58"
      ]
    },
    {
      "id": "second_order_meta",
      "severity": "medium",
      "title": "Tank damage nerf may reduce frontline presence",
      "evidence": ["char_A role: tank", "pick_rate_low: 0.22"]
    }
  ],
  "solution_paths": [
    {
      "type": "comms_only",
      "confidence": "medium",
      "rationale": "Data does not support buff; address perception first",
      "designer_decides": true
    },
    {
      "type": "targeted_by_bracket",
      "confidence": "medium",
      "rationale": "If change needed, target low-bracket abuse only — not global buff",
      "designer_decides": true
    },
    {
      "type": "kit_redesign",
      "confidence": "low",
      "rationale": "If playtests confirm 'unfun' not 'weak' — review shield skill reward",
      "designer_decides": true
    }
  ],
  "validation_plan": [
    "Survey: fun to play vs feels weak",
    "Playtest char_A in low vs high bracket",
    "Monitor pick rate 1 week if micro-patch ships"
  ]
}
```

---

## Layer 7 — Insight + Report (LLM)

### What it does

- Takes structured output from L3–L6 (numbers + insights already computed)
- LLM writes **human-readable report**: summary, narrative, draft player comms
- Every claim must reference insight IDs / evidence from previous layers

### What it does NOT do

- Does not calculate win rates
- Does not invent stats or player quotes
- Does not decide the patch — **designer decides**

### Input to LLM (structured JSON only)

```json
{
  "metrics_summary": { "char_A": { "wr_low": 0.58, "wr_high": 0.49 } },
  "impact": { },
  "risks": [ ],
  "solution_paths": [ ],
  "context": {
    "proposed_change": "Ironclad damage 45 → 40 (-11%)"
  }
}
```

### Example output — Report sections

```markdown
# Patch Brief — v0.4.3

## Executive Summary
Ironclad (char_A) shows strong performance in low bracket (58% WR) but
near-average in high bracket (49%). Community sentiment ("feels weak", 340
mentions) diverges from data. Proposed damage nerf may worsen perception
without addressing root cause.

## Who Is Affected
- Low bracket players using Ironclad (high pick share, high WR)
- Tank role team compositions

## Risks
1. **Stakeholder conflict (high)** — Players request buff; data shows already strong in casual play
2. **Second-order meta (medium)** — Tank nerfs may shift team comp diversity

## Suggested Paths (designer decides)
1. **Comms first** — Explain current performance data to community
2. **Bracket-targeted tune** — Only if abuse confirmed in low bracket
3. **Kit review** — If "unfun" not "weak" in playtest surveys

## Draft Player Communication
"We've seen feedback on Ironclad. Our data shows performance near average
in skilled play; we're reviewing game feel and clarity before making balance
changes."
```

**Fallback:** If LLM fails, generate report from template using L6 JSON directly.

---

## Full Walkthrough — One Patch Decision

**Scenario:** MOBA game. Community says Ironclad (char_A) is weak. Designer planned damage nerf. Is this correct?

| Layer | What happens |
|-------|--------------|
| **L0** | Studio logs mapped: `Death` → `death`, `characterId` → `entity_id` |
| **L1** | 500 live + 50 playtest sessions validated and sorted |
| **L2** | Low bracket players use Ironclad shield often; high bracket kited more |
| **L3** | WR low 58%, WR high 49% — strong casual, average ranked |
| **L4** | Ironclad = tank, 45 damage. Plan: nerf to 40. Community: "feels weak" × 340 |
| **L5** | Pattern: `bracket_split_easy_low` + `perception_vs_data_divergence` |
| **L6** | Risk: `stakeholder_conflict`. Suggest: `comms_only` or `kit_redesign`, NOT blind nerf |
| **L7** | Report explains mismatch and recommends designer investigate feel before numbers |

**Conclusion:** Raw nerf was wrong direction. Tool caught it **before ship**.

---

## Layer Summary Table

| Layer | Name | Input | Output | AI? |
|-------|------|-------|--------|-----|
| L0 | Adapter | Raw studio JSON | Canonical events | Suggest only at setup |
| L1 | Ingest & Normalize | Canonical events | Clean event stream | No |
| L2 | Gameplay Semantic | Events | Session models, behavior flags | No |
| L3 | Metric & Cohort | Session models | Win rate, pick rate, etc. | No |
| L4 | Context | Game def, rules, plan, community | Context bundle | No |
| L5 | Impact & Alignment | Metrics + context | Who affected, patterns | No |
| L6 | Risk & Solutions | L5 insights | Risks, solution paths | No |
| L7 | Report | L3–L6 JSON | Markdown report | **LLM yes** |

---

## What We Tell Judges

> "We don't auto-balance games. We run player data through a deterministic pipeline, merge it with game definition and community context, detect alignment gaps and risks, then use AI only to write the briefing. Designers make the final call."

---

## Related Files

- `Architect.png` — system diagram
- `api-list.md` — backend endpoints
- `ui-screen-list.md` — frontend screens
- `game-definition-context-layer.md` — game definition detail
- `tech-stack.md` — React + ASP.NET Core + Azure
