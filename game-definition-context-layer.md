# Game Definition — Missing Piece in Context Layer

**Problem:** Context layer has Rules, Update Plan, and Community — but no data that describes **what the game is**: roster, character stats, roles, mechanics. Without this, AI cannot meaningfully judge patches (e.g. “nerf 10%” of what? `char_A` is tank or assassin?).

---

## What to Add

Add to **Context Layer (L4)**:

| File / module | Suggested name | Contains |
|---------------|----------------|----------|
| **Game manifest** | `game_manifest.json` | Game name, genre, mode, bracket definitions |
| **Content catalog** | `content_catalog.json` | Characters, weapons, items, levels — current roster |
| **Entity stats** | `entity_stats.json` | HP, damage, cooldown, role, tags |
| **Mechanic registry** | `mechanics.json` | Dodge, ult, passive — short descriptions |

Can be merged into one file: **`game_definition.json`** (also called **Design Snapshot**).

```
Context Layer =
  Game Definition   ← NEW (what exists in the game)
  Rules             (how to judge / locked levers)
  Update Plan       (what will change)
  Community         (what players say)
```

---

## Why This Matters for AI / Pipeline

| Without game data | With game data |
|-------------------|----------------|
| “Nerf char_A 10%” — 10% of what? | Base damage 100 → 90, role = DPS |
| “WR low” — compared to 20 or 5 heroes? | Pick rate 8% / 20 chars = niche pick |
| Community says “Z is weak” | Z = support, low damage by design |
| Second-order risk unclear | Nerf tank → team comp meta shift |

Layers L5–L6 need to join: `metrics.entity_id` ↔ `catalog.entities[id]`.

---

## Where Studios Get This Data (Real World)

| Source | Who provides | Common format |
|--------|--------------|---------------|
| **Data tables / ScriptableObjects** (Unity) | Dev one-click export | JSON / CSV |
| **Excel / Google Sheet** (design bible) | Game designer | CSV → JSON |
| **GDD excerpt** | Designer | Manual → structured JSON (hackathon) |
| **Game config / balance sheet** | Live ops | `balance_v0.4.2.json` |
| **CMS / content pipeline** | Large studios | API (stretch) |
| **Update plan** (partial only) | Designer | Delta only — **not** full roster |

**Reality:** Studios already have balance tables — they don’t **combine** them with telemetry + community in one place. That’s what this tool does.

---

## What NOT to Do

- ❌ Let LLM read logs and guess “char_A is probably DPS”
- ❌ Scrape wiki as source of truth

- ✅ Designer/dev **uploads Design Snapshot** (or exports from engine)
- ✅ AI **uses** catalog to interpret — does not invent stats

Same principle as adapter mapping: **game data = structured input**, not AI output.

---

## Suggested Structure (`game_definition.json`)

```json
{
  "game_id": "arena_moba",
  "genre": "MOBA",
  "version": "0.4.2",
  "brackets": [
    { "id": "bronze", "label": "Low elo" },
    { "id": "diamond", "label": "High elo" }
  ],
  "entities": [
    {
      "id": "char_A",
      "name": "Ironclad",
      "type": "character",
      "role": "tank",
      "tags": ["frontline", "beginner_friendly"],
      "stats": {
        "hp": 1200,
        "base_damage": 45,
        "cooldown_q": 8
      },
      "intentional_difficulty": "easy"
    },
    {
      "id": "char_B",
      "name": "Vex",
      "type": "character",
      "role": "assassin",
      "tags": ["high_skill_ceiling"],
      "stats": { "hp": 800, "base_damage": 85 }
    }
  ],
  "mechanics": [
    { "id": "dodge", "name": "Dodge roll", "introduced_in": "tutorial" }
  ]
}
```

**Update plan** only needs to reference `entity_id`:

```json
{
  "changes": [
    { "target": "char_A", "field": "base_damage", "from": 45, "to": 40, "delta": "-11%" }
  ]
}
```

Pipeline joins plan + catalog → **“Ironclad (tank) damage 45→40”**.

---

## Architecture Placement

```
User uploads:
  game_definition.json   ← NEW
  rules.json
  update_plan.json
  community.json
  player_online.json
  player_offline.json
  adapter.json

Backend L1–L3 → metrics
Backend L4    → merge game_definition + rules + update_plan + community
              → context_bundle.json
L4 + metrics  → L5 Impact & Alignment → L6 Risk → L7 Report
```

**Validation:** `update_plan.target` and `metrics.entity_id` must exist in `game_definition.entities` — fail early if IDs don’t match.

---

## UX — Don’t Make Designers Type the Whole Roster

| Approach | UX |
|----------|-----|
| **Upload one file** `game_definition.json` | Export from balance sheet |
| **Template + short form** | Hackathon: enter 5 main heroes |
| **Unity export button** | Stretch: `GameDefinitionExporter.cs` → JSON |
| **Import from update plan only** | ❌ Not enough — plan has no full stats |
| **AI from GDD PDF** | Optional onboarding — human confirms (like adapter) |

**Hackathon MVP:** one `game_definition.json` in `fixtures/` + doc: “export from your balance sheet”.

---

## Four Context Types (Don’t Mix Them Up)

| Type | Question it answers | Example |
|------|---------------------|---------|
| **Game definition** | What does the game *have*? Current stats? | 12 chars, char_A tank 45 dmg |
| **Rules** | What *can* change? | Sniper: don’t change headshot |
| **Update plan** | What *will* change? | char_A -11% damage |
| **Community** | What do players *think*? | “char_A feels weak” |

AI judgment = **metrics + definition + plan + rules + community** together.

---

## Team Ownership (24h Hackathon)

| Member | Task |
|--------|------|
| **B — Context** | Own `game_definition.json` schema + fixture with 5–10 entities |
| **Designer on team** (if any) | Fill roles, stats, tags |
| **E — Integration** | Validate entity IDs across all upload files |

**Updated upload contract:**

```
fixtures/demo_case/
  game_definition.json   ← NEW
  player_online.json
  player_offline.json
  rules.json
  update_plan.json
  community.json
  adapter.json
```

---

## Stretch: Unity / C# Export (Fits Our Stack)

ScriptableObject or JSON in `Resources/`:

```
BalanceData/Characters.asset  →  export  →  game_definition.json
```

Designer clicks **“Export for Patch Brief”** once per build. No live API needed.

---

## Judge Q&A

**Q: How do you know how many characters the game has?**  
> Designer uploads **Game Definition** — roster + stats snapshot. Same as their balance sheet, standardized as JSON.

**Q: Every game is different?**  
> Canonical **shape** (entities, stats, roles); content filled per studio — same idea as log adapters.

**Q: Does AI read the game by itself?**  
> No. AI **interprets** using the uploaded catalog — avoids hallucinated stats.

---

## Summary

| Problem | Solution |
|---------|----------|
| Context missing “what is the game” | Add **`game_definition.json`** to L4 |
| Where to get it | Balance sheet / Unity export / designer template |
| How AI uses it | Join `entity_id`, explain % changes, role-aware risks |
| Hackathon | One fixture file + ID validation |

**Full Context layer:**

```
Game Definition  +  Rules  +  Update Plan  +  Community
        ↓
   context_bundle → L5–L7
```
