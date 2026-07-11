# Golden Demo Case - Balance & Patch Decision Brief

This directory contains a complete, valid set of the 5 input files representing a typical balance decision scenario for a MOBA game (`arena_moba`) expected by the C# backend.

## Roster & Balance Narrative
- **`char_A` (Ironclad - Tank)**: 
  - **The Narrative**: A beginner-friendly tank that is highly dominant in low skill tiers (casual/bronze, ~80% win rate) because players don't know how to kite him, but is average/balanced in high skill tiers (diamond, ~50% win rate).
  - **The Problem**: The community is complaining heavily that Ironclad "feels weak" (340 negative mentions). 
  - **The Update Plan**: The designers planned a flat base damage nerf (45 -> 40, a -11% decrease).
  - **The Conflict**: Data does not support a nerf in skilled play, and a nerf might worsen the negative community perception.

- **`char_B` (Vex - Assassin)**:
  - **The Narrative**: A high-skill-ceiling assassin.
  - **The Problem**: Extremely weak in low skill tiers (0% win rate) but highly dominant in high skill tiers (83.3% win rate).
  - **The Conflict**: High elo meta-dominant, triggering a clear bracket split.

---

## File Manifest

1. **`game_definition.json`**: Roster (Ironclad, Vex) and bracket rating thresholds (Bronze < 1000, Diamond >= 1000).
2. **`adapter.json`**: L0 Adaptive field mappings. Converts studio-specific raw keys (`hero`, `bracket`, `wr`, `pr`, `dr`) to canonical schema keys (`entity_id`, `bracket_id`, `win_rate`, `pick_rate`, `death_rate`).
3. **`telemetry_live.json`**: Structured live match telemetry using raw keys.
4. **`telemetry_playtest.json`**: Structured playtest match telemetry using raw keys.
5. **`context_bundle.json`**: Pre-merged L4 Context Bundle containing `game_definition`, `rules`, `update_plan`, `community` sentiment clusters, and `joined_changes`.
