# 24h Team Plan — Balance / Patch Decision Brief Tool

**Team size:** 5 members  
**Architecture:** `Architect.png` — WEB (Ingest → Semantic → Metric → Context) → AI (Impact → Risk → Report) → User

**Assumption:** 24h straight hackathon (or 2×12h). Adjust sleep if your event allows breaks.

---

## Team Roles (assign at Hour 0\)

| Role | Owner | Owns |
| :---- | :---- | :---- |
| **A — Pipeline** | Member 1 | L1 Ingest & Normalize, L2 Semantic, L3 Metric |
| **B — Context** | Member 2 | L4 Rules, Update Plan, Community processor \+ **all mock JSON** |
| **C — AI Engine** | Member 3 | L5 Impact & Alignment, L6 Risk & Solutions, LLM prompt |
| **D — Frontend** | Member 4 | Upload UI, report view, demo flow |
| **E — Lead / Integration** | Member 5 | Repo, API wiring, E2E, pitch deck, demo script |

Everyone reviews **`data_contract.md`** at Hour 0 — no coding until schemas are frozen.

---

## Hour-by-Hour Timeline

### Phase 0 — Setup (Hour 0–2) — **whole team**

| Time | Task | Owner | Done when |
| :---- | :---- | :---- | :---- |
| 0:00 | Repo, branches, `.env.example`, README skeleton | E | GitHub up |
| 0:30 | Freeze **JSON schemas** (events, metrics, context, report) | E \+ A \+ B | `schemas/` committed |
| 1:00 | Create **1 golden demo case** (Gears-style bracket split) | B | `fixtures/demo_case/` |
| 1:30 | Role split \+ API contract (`POST /analyze`) | All | Notion/sheet with tasks |
| 2:00 | **Mock data v0** committed | B | Pipeline can run on files |

**Golden rule:** No LLM until Hour 10\. Layers 1–6 output JSON first.

---

### Phase 1 — Parallel Build (Hour 2–10)

#### Member A — Pipeline (L1–L3)

| Hour | Deliverable |
| :---- | :---- |
| 2–4 | L1: parse online \+ offline logs, validate, tag `source` |
| 4–6 | L2: segments, bracket profiles, 3 behavior flags |
| 6–8 | L3: WR/pick rate/death rate by bracket \+ entity |
| 8–10 | Unit test on `fixtures/` → output `metrics.json` |

**MVP cut:** 6 event types only: `session_start`, `match_end`, `death`, `ability_used`, `entity_pick`, `area_enter`.

---

#### Member B — Context (L4)

| Hour | Deliverable |
| :---- | :---- |
| 2–4 | `rules.json` (locked/open levers per entity) |
| 4–6 | `update_plan.json` parser (buff/debuff/mechanic) |
| 6–8 | Community: 40–60 mock posts → cluster by entity \+ theme |
| 8–10 | `context_bundle.json` merger API |

**MVP cut:** No real scrape — CSV/JSON mock labeled `reddit_mock.csv`. Real scrape \= stretch.

---

#### Member C — AI Logic (L5–L6, no LLM yet)

| Hour | Deliverable |
| :---- | :---- |
| 2–5 | 4 impact rules (bracket split, plan vs data, playtest vs live gap) |
| 5–8 | 4 risk types \+ 4 solution path templates |
| 8–10 | `insights.json` from metrics \+ context (deterministic) |

**Must-have patterns:**

- `bracket_split_easy_low`  
- `perception_vs_data_divergence`  
- `identity_lever_conflict`  
- `second_order_meta_risk`

---

#### Member D — Frontend

| Hour | Deliverable |
| :---- | :---- |
| 2–5 | Upload page (drag JSON/zip) |
| 5–8 | Loading state \+ error display |
| 8–10 | Report layout (sections from output spec) |

**Stack suggestion:** React/Vite or Next — whatever team knows fastest.

---

#### Member E — Integration

| Hour | Deliverable |
| :---- | :---- |
| 2–4 | FastAPI/Express skeleton, `POST /analyze` |
| 4–7 | Wire A's pipeline as steps 1–3 |
| 7–10 | Wire B's context \+ C's rules → single response JSON |

---

### Phase 2 — Integration (Hour 10–16)

| Time | Task | Owners |
| :---- | :---- | :---- |
| 10:00 | **Sync \#1** — pipeline end-to-end without LLM | All |
| 10–12 | C adds LLM report layer (1 call, JSON in → markdown out) | C \+ E |
| 12–13 | **Break / food** | All |
| 13–14 | D connects UI to real API | D \+ E |
| 14–15 | Fix schema mismatches | A \+ B \+ E |
| 15–16 | **Sync \#2** — full demo path on golden case | All |

**Hour 16 checkpoint:** User uploads fixtures → sees report. If not, **drop stretch goals** (see Cut list).

---

### Phase 3 — Polish & Pitch (Hour 16–22)

| Time | Task | Owner |
| :---- | :---- | :---- |
| 16–17 | Second demo case (Reddit "feels weak, data fine") | B |
| 17–18 | UI polish: executive summary, risk badges, confidence | D |
| 18–19 | Error handling \+ empty states | D \+ E |
| 19–20 | **Pitch deck** (8 slides max) | E |
| 20–21 | Record 2-min demo video (backup) | E \+ D |
| 21–22 | Rehearse live demo 3× | All |

---

### Phase 4 — Buffer (Hour 22–24)

| Time | Task |
| :---- | :---- |
| 22–23 | Bug fixes only — no new features |
| 23–24 | Deploy (Railway/Vercel), final README, submit |

---

## Sync Points (non-negotiable)

| When | Duration | Agenda |
| :---- | :---- | :---- |
| Hour 0 | 30 min | Schemas \+ golden case |
| Hour 10 | 20 min | Integration status, blockers |
| Hour 16 | 20 min | Demo go/no-go |
| Hour 21 | 30 min | Pitch rehearsal |

Use one Slack/Discord thread: **blockers only**, no long debates.

---

## Data Contract (freeze at Hour 1\)

fixtures/

  demo\_case/

    player\_online.json      \# live telemetry aggregate or events

    player\_offline.json     \# playtest sessions

    rules.json

    update\_plan.json

    community.json

  demo\_case\_2/              \# stretch: perception gap

**API:**

POST /analyze

Content-Type: multipart/form-data

files: player\_online, player\_offline, rules, update\_plan, community

→ 200 { metrics, insights, risks, solutions, report\_markdown }

---

## Report Output Structure (C \+ D align on this)

{

  "executive\_summary": "...",

  "who\_is\_affected": \[

    { "cohort": "low\_bracket", "entity": "char\_A", "impact": "high" }

  \],

  "alignment": {

    "data\_vs\_community": "divergent",

    "playtest\_vs\_live": "aligned"

  },

  "risks": \[

    { "id": "stakeholder\_conflict", "severity": "high", "evidence": \[\] }

  \],

  "solution\_paths": \[

    { "type": "targeted\_by\_bracket", "confidence": "medium", "rationale": "..." }

  \],

  "validation\_plan": \["..."\],

  "report\_markdown": "..."

}

---

## Cut List (if behind schedule)

| Priority | Keep | Cut |
| :---- | :---- | :---- |
| P0 | Golden case, 1 upload, deterministic insights, LLM report | — |
| P1 | 2nd demo case, confidence badges | Real community scrape |
| P2 | Playtest vs live compare | Fancy charts |
| P3 | Auth, DB, queue | Unity plugin |

**Never cut:** L5–L6 rules, evidence on every insight, "not auto-balance" in pitch.

---

## Demo Script (3 minutes) — E owns

1. **Problem (20s):** "Dashboards show numbers; they don't bridge dev, community, and patch plans."  
2. **Upload (30s):** Golden case files.  
3. **Who affected (40s):** "Char A — strong low elo, mediocre high; plan buffs damage → conflict."  
4. **Alignment (40s):** "Community says weak; data near average → kit/feel, not numbers."  
5. **Risks (30s):** Stakeholder conflict \+ second-order meta.  
6. **Solutions (30s):** Bracket-targeted tune vs comms vs iterate — **designer decides**.  
7. **Close (10s):** "Machinations designs systems; GameAnalytics measures; we frame patch decisions."

---

## Pitch Deck Outline (8 slides)

1. Problem (Reddit \+ GMTK quote)  
2. Pain: data ≠ community ≠ right fix type  
3. Architecture (`Architect.png`)  
4. Inputs (online, offline, rules, plan, community)  
5. How it works (WEB deterministic → AI narrative)  
6. Demo screenshot  
7. vs Machinations / GameAnalytics  
8. Roadmap \+ team

---

## Tech Stack (pick one, Hour 0\)

| Layer | Recommendation |
| :---- | :---- |
| API | **Python FastAPI** (fast for JSON pipeline) |
| Pipeline | Plain functions per layer, no framework |
| LLM | OpenAI / Gemini API, 1 structured prompt |
| Frontend | React \+ Vite |
| Deploy | Backend Railway, Frontend Vercel |

---

## Definition of Done (Hour 16\)

- [ ] Upload 5 files → report in \< 60s  
- [ ] Every risk cites `evidence`  
- [ ] Report has: who affected, risks, solution paths  
- [ ] Works without internet except LLM call  
- [ ] README: how to run \+ demo files  
- [ ] Pitch deck ready

---

## Risk Mitigations

| Risk | Mitigation |
| :---- | :---- |
| Schema drift | E owns `schemas/`, PR review only E |
| LLM slow/fail | Template fallback report from `insights.json` |
| Pipeline empty | B delivers fixtures by Hour 2 |
| Frontend blocked | E returns raw JSON; pretty UI later |
| Scope creep | E says no to new features after Hour 16 |

---

## Optional Stretch (only if Done early)

- Chart: WR by bracket (Recharts)  
- Export PDF report  
- "Compare two update plans"  
- Lightweight Reddit scraper (1 subreddit, 50 posts)

---

## Summary

| Phase | Hours | Focus |
| :---- | :---- | :---- |
| Setup | 0–2 | Schemas, mock data, roles |
| Parallel build | 2–10 | Each member owns their layer |
| Integration | 10–16 | API glue, LLM, UI |
| Polish & pitch | 16–22 | Demo, deck, rehearsal |
| Buffer | 22–24 | Deploy, fix, submit |

**Critical path:** Member B's mock data \+ Member E's integration — protect those first.  
