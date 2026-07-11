# Golden Demo Case - Balance & Patch Decision Brief

This directory contains a complete, valid set of the 7 input files representing a typical balance decision scenario for a MOBA game (`arena_moba`).

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

1. **`game_definition.json`**: Roster (Ironclad, Vex), bracket rating thresholds (Bronze < 1000, Diamond >= 1000), and base stats.
2. **`adapter.json`**: L0 Adaptive field mappings. Converts studio-specific raw keys (`hero`, `time`, `rank`, `player`, `match`, `event`) to canonical schema keys (`entity_id`, `timestamp`, `bracket_id`, `player_id`, `match_id`, `event_type`).
3. **`player_online.json`**: Live telemetry event stream (15 matches, 50+ events) utilizing custom studio keys.
4. **`player_offline.json`**: Playtest telemetry event stream (4 matches) utilizing custom studio keys.
5. **`rules.json`**: Lever rules. Declares `base_damage` as an open lever for `char_A`, but `identity_skill_shield` as locked.
6. **`update_plan.json`**: Details the proposed base damage nerf for `char_A` from 45 to 40.
7. **`community.json`**: Cluster analysis of community sentiment, showing the negative sentiment and quotes for both characters.
