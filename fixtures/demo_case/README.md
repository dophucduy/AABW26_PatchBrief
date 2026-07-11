# Golden Demo Case - Balance & Patch Decision Brief

This directory contains a complete, valid set of input files representing a typical balance decision scenario for a MOBA game (`arena_moba`).

## Roster & Balance Narrative
- **`char_A` (Ironclad - Tank)**: 
  - **The Narrative**: A beginner-friendly tank that is highly dominant in low skill tiers (casual/bronze, ~80% win rate) because players don't know how to kite him, but is average/balanced in high skill tiers (diamond, ~50% win rate).
  - **The Problem**: The community is complaining heavily that Ironclad "feels weak" (340 negative mentions). 
  - **The Update Plan**: The designers planned a flat base damage nerf (45 -> 40, a -11% decrease).
  - **The Conflict**: Data does not support a nerf in skilled play, and a nerf might worsen the negative community perception. Also, it touches an "open" lever (`base_damage`), which is safe, but may cause stakeholder/design conflicts.

- **`char_B` (Vex - Assassin)**:
  - **The Narrative**: A high-skill-ceiling assassin.
  - **The Problem**: Extremely weak in low skill tiers (0% win rate) but highly dominant in high skill tiers (83.3% win rate).
  - **The Conflict**: High elo meta-dominant, triggering a clear bracket split.

---

## File Manifest

### API inputs (L0–L7 pipeline)

1. **`game_definition.json`**: Balance config — roster (Ironclad, Vex), bracket thresholds, and base stats.
2. **`adapter.json`**: L0 metric mappings. Converts studio export keys (`hero`, `bracket`, `wr`, `pr`) to canonical telemetry fields (`entity_id`, `bracket_id`, `win_rate`, `pick_rate`).
3. **`telemetry_live.json`**: Structured live match telemetry — win rate, pick rate, sessions per entity × bracket (GameAnalytics-style aggregate, not raw event logs).
4. **`telemetry_playtest.json`**: Structured playtest telemetry in the same aggregate format.
5. **`context_bundle.json`**: Produced by L4 (Context Layer). Merges game definition, rules, update plan, community sentiment, and joined changes for L5–L7.

### L4 source inputs (not consumed directly by L5–L7)

6. **`rules.json`**: Lever rules. Declares `base_damage` as an open lever for `char_A`, but `identity_skill_shield` as locked.
7. **`update_plan.json`**: Details the proposed base damage nerf for `char_A` from 45 to 40.
8. **`community.json`**: Cluster analysis of community sentiment, showing the negative sentiment and quotes for both characters.
