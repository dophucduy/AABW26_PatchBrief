# Software Requirements Specification (SRS)

## Patchframe — Pre-Ship Balance Decision Brief

| Field | Value |
|-------|-------|
| **Project name** | Patchframe |
| **Tagline** | *Frame the patch before you ship* |
| **Version** | 1.0 (Hackathon MVP) |
| **Date** | July 2026 |
| **Document type** | Software Requirements Specification |

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Problem Statement](#2-problem-statement)
3. [Goals & Non-Goals](#3-goals--non-goals)
4. [Stakeholders & Users](#4-stakeholders--users)
5. [System Overview](#5-system-overview)
6. [Functional Requirements](#6-functional-requirements)
7. [Data Requirements](#7-data-requirements)
8. [Pipeline Architecture](#8-pipeline-architecture)
9. [User Interface Requirements](#9-user-interface-requirements)
10. [API Requirements](#10-api-requirements)
11. [Non-Functional Requirements](#11-non-functional-requirements)
12. [Technology Stack](#12-technology-stack)
13. [Scope — In & Out](#13-scope--in--out)
14. [User Flows](#14-user-flows)
15. [Comparison with Existing Tools](#15-comparison-with-existing-tools)
16. [Risks & Mitigations](#16-risks--mitigations)
17. [Success Criteria](#17-success-criteria)
18. [Related Documents](#18-related-documents)

---

## 1. Introduction

### 1.1 Purpose

This document describes the software requirements for **Patchframe**, a web-based decision-support tool for game studios. Patchframe helps designers and live-ops teams evaluate balance patches **before shipping** by combining player telemetry, playtest data, game definition, design rules, community sentiment, and update plans into a single **impact brief**.

### 1.2 Product Summary

Patchframe does **not** auto-balance games. It **frames the problem**: who is affected, where data and community diverge, what risks exist, and what types of solutions fit — so designers make informed decisions.

### 1.3 One-Line Pitch

> *"Machinations designs systems. GameAnalytics measures players. Patchframe frames patch decisions across data, design, and community."*

---

## 2. Problem Statement

### 2.1 Industry Pain Points

| Pain point | Description |
|------------|-------------|
| **Data without interpretation** | Studios have telemetry (e.g. GameAnalytics) but lack analysts to answer "what if we ship this nerf?" |
| **Community vs data conflict** | Players say "underpowered"; data shows average performance — issue may be kit feel, not numbers |
| **Bracket blindness** | A character strong in low elo and weak in high elo needs different fixes than a flat win-rate nerf |
| **Wrong fix type** | Teams tune numbers when the real problem is onboarding, UI, or kit design (GMTK root-cause problem) |
| **Playtest vs live gap** | Offline playtest results may not match live meta |
| **No dev–player bridge** | Design meetings lack a structured view of community narrative alongside metrics |
| **Heterogeneous log formats** | Each studio logs events differently — no standard ingest |

### 2.2 What Existing Tools Do Not Solve

| Tool | Gap |
|------|-----|
| **Machinations** | Design-time simulation — not live player data or community |
| **GameAnalytics** | Shows *what happened* — not *what if we ship this patch* or *how to interpret bracket splits* |
| **Raw dashboards** | Require analysts; no patch-decision workflow |
| **Community forums** | Loud, emotional, unstructured — no join with telemetry |

---

## 3. Goals & Non-Goals

### 3.1 Goals

| ID | Goal |
|----|------|
| G1 | Ingest online (live) and offline (playtest) player data via configurable adapters |
| G2 | Merge game definition, rules, update plan, and community context |
| G3 | Detect alignment patterns (data vs community, playtest vs live, bracket splits) |
| G4 | Surface risks and solution **paths** (not prescriptions) |
| G5 | Generate human-readable report with evidence |
| G6 | Support first-time studio onboarding via AI-assisted field mapping |

### 3.2 Non-Goals (MVP)

| ID | Non-goal |
|----|----------|
| NG1 | Auto-apply balance changes to game builds |
| NG2 | Replace GameAnalytics or Machinations |
| NG3 | Train custom ML models |
| NG4 | Real-time 24/7 live ops monitoring |
| NG5 | Full economy simulation |
| NG6 | Player-facing features or account system |

---

## 4. Stakeholders & Users

### 4.1 Primary Users

| User | Role | How they use Patchframe |
|------|------|-------------------------|
| **Combat / Balance Designer** | Proposes patches | Upload plan + data → read impact brief |
| **Live Ops Lead** | Ships patches | Assess risk before deploy |
| **Community Manager** | Player comms | Use draft patch notes and backlash risk |
| **QA Lead** | Test planning | Use validation checklist from report |
| **Producer** | Go/no-go | Executive summary for patch meetings |

### 4.2 Secondary Users

| User | Role |
|------|------|
| **Indie developer** | No data analyst — uses tool as "analyst in a box" |
| **Hackathon judges** | Evaluate demo with fixture data |

---

## 5. System Overview

### 5.1 High-Level Architecture

```
USER
  │
  ▼ upload files
WEB (React)
  │
  ▼ REST API
BACKEND (ASP.NET Core on Azure)
  │
  ├── WEB Pipeline (deterministic C#)
  │     L0 Adapter → L1 Ingest → L2 Semantic → L3 Metric → L4 Context
  │
  └── AI Pipeline
        L5 Impact & Alignment → L6 Risk & Solutions → L7 Report (LLM)
  │
  ▼
REPORT → displayed in WEB
```

### 5.2 System Boundary

| Inside Patchframe | Outside (user provides) |
|-------------------|-------------------------|
| Ingest, normalize, analyze | Raw game logs |
| Mapping UI + adapter storage | Game definition / balance sheet |
| Report generation | Community posts or scrape URL |
| Swagger API | Unity / GameAnalytics export |

### 5.3 Deployment

| Component | Host |
|-----------|------|
| Frontend | Vercel / Netlify |
| Backend API | Azure App Service |
| LLM | OpenAI / Gemini API (cloud) |
| Database | None (MVP — file upload, in-memory adapter storage) |

---

## 6. Functional Requirements

### 6.1 Data Mapping (Layer 0)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-0.1 | System shall accept sample JSON logs from user | P1 |
| FR-0.2 | System shall suggest field and event mappings to canonical schema via AI | P1 |
| FR-0.3 | User shall review and correct mappings before save | P1 |
| FR-0.4 | System shall show confidence per mapping row (high / medium / low) | P1 |
| FR-0.5 | System shall save confirmed adapter for reuse in analyze runs | P1 |
| FR-0.6 | Canonical schema shall be defined by the tool (not user-invented targets) | P0 |
| FR-0.7 | User may optionally define custom fields (stretch) | P2 |

### 6.2 Ingest & Normalize (Layer 1)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1.1 | System shall validate uploaded JSON against schema | P0 |
| FR-1.2 | System shall support `source: live` and `source: playtest` | P0 |
| FR-1.3 | System shall apply saved adapter to raw logs | P0 |
| FR-1.4 | System shall reject invalid timestamps and missing required fields | P0 |

### 6.3 Gameplay Semantic (Layer 2)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-2.1 | System shall build session segments from events | P0 |
| FR-2.2 | System shall derive bracket behavior profiles | P0 |
| FR-2.3 | System shall flag behavior patterns (e.g. asymmetric feature usage) | P1 |

### 6.4 Metrics (Layer 3)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-3.1 | System shall compute win rate, pick rate, death rate by entity | P0 |
| FR-3.2 | System shall segment metrics by skill bracket | P0 |
| FR-3.3 | System shall separate live vs playtest metrics | P0 |

### 6.5 Context (Layer 4)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-4.1 | System shall ingest `game_definition.json` (roster, stats, roles) | P0 |
| FR-4.2 | System shall ingest `rules.json` (locked / open levers) | P0 |
| FR-4.3 | System shall ingest `update_plan.json` (proposed changes) | P0 |
| FR-4.4 | System shall ingest `community.json` (sentiment clusters) | P0 |
| FR-4.5 | System shall validate entity IDs across files | P0 |
| FR-4.6 | System shall join update plan with game definition (before/after values) | P0 |
| FR-4.7 | System may scrape community data via Scrap AI module | P2 |

### 6.6 Impact & Alignment (Layer 5)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-5.1 | System shall identify who is affected (cohort × entity) | P0 |
| FR-5.2 | System shall detect bracket split patterns | P0 |
| FR-5.3 | System shall detect data vs community divergence | P0 |
| FR-5.4 | System shall detect playtest vs live mismatch | P1 |
| FR-5.5 | System shall detect update plan conflicts with data | P0 |
| FR-5.6 | All insights shall include evidence and confidence | P0 |
| FR-5.7 | Layer 5 shall not use LLM (rule-based only) | P0 |

### 6.7 Risk & Solutions (Layer 6)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-6.1 | System shall classify risks (stakeholder conflict, second-order meta, etc.) | P0 |
| FR-6.2 | System shall suggest solution **paths** (not auto-patch commands) | P0 |
| FR-6.3 | All solution paths shall be marked `designer_decides: true` | P0 |
| FR-6.4 | System shall output validation plan for next playtest | P0 |
| FR-6.5 | Layer 6 shall not use LLM (rule-based only) | P0 |

### 6.8 Report (Layer 7)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-7.1 | System shall generate executive summary | P0 |
| FR-7.2 | System shall generate markdown report from structured insights | P0 |
| FR-7.3 | System shall generate draft player-facing communication | P1 |
| FR-7.4 | LLM shall only use precomputed JSON — not raw logs | P0 |
| FR-7.5 | System shall fall back to template report if LLM unavailable | P0 |

### 6.9 General

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-9.1 | System shall expose Swagger UI for API testing | P0 |
| FR-9.2 | System shall provide demo mode with fixture data | P0 |
| FR-9.3 | System shall support multipart file upload | P0 |

---

## 7. Data Requirements

### 7.1 Upload Files (Analyze)

| File | Required | Description |
|------|----------|-------------|
| `player_online.json` | Yes | Live telemetry (events or aggregates) |
| `player_offline.json` | Yes | Playtest session data |
| `game_definition.json` | Yes | Roster, stats, roles, mechanics |
| `rules.json` | Yes | Locked / open design levers |
| `update_plan.json` | Yes | Proposed buffs, nerfs, mechanic changes |
| `community.json` | No* | Sentiment clusters or raw posts |
| `adapter.json` | Yes** | Field / event mapping |

\*Required unless Scrap AI used  
\*\*Or reference saved `adapterId`

### 7.2 Game Definition Schema (minimum)

```json
{
  "game_id": "string",
  "genre": "string",
  "version": "string",
  "entities": [
    {
      "id": "string",
      "name": "string",
      "role": "string",
      "stats": {},
      "tags": []
    }
  ],
  "mechanics": []
}
```

### 7.3 Canonical Event Types (after adapter)

`death`, `match_end`, `ability_used`, `entity_pick`, `area_enter`, `session_start`

### 7.4 Canonical Fields

`t`, `entity_id`, `session_id`, `bracket`, `area_id`, `cause_id`, `source`

### 7.5 Report Output (minimum)

- `executive_summary`
- `who_is_affected[]`
- `proposed_changes[]`
- `alignment`
- `risks[]`
- `solution_paths[]`
- `validation_plan[]`
- `report_markdown`

---

## 8. Pipeline Architecture

| Layer | Name | Technology | AI? |
|-------|------|------------|-----|
| L0 | Adapter | C# + optional LLM at setup | Suggest only |
| L1 | Ingest & Normalize | C# | No |
| L2 | Gameplay Semantic | C# | No |
| L3 | Metric & Cohort | C# | No |
| L4 | Context | C# | No |
| L5 | Impact & Alignment | C# rules | No |
| L6 | Risk & Solutions | C# rules | No |
| L7 | Insight + Report | LLM API | Yes |

> **Detailed layer examples:** see `pipeline-layers-explained.md`

---

## 9. User Interface Requirements

### 9.1 Screens (MVP)

| Screen | Route | Priority |
|--------|-------|----------|
| Home | `/` | P0 |
| Mapping — Upload sample | `/mapping` | P1 |
| Mapping — Review | `/mapping/review` | P1 |
| Mapping — Confirm | `/mapping/confirm` | P1 |
| New Analysis | `/analyze` | P0 |
| Processing | `/analyze/loading` | P0 |
| Report | `/report` | P0 |

### 9.2 Report Sections

1. Executive summary + risk badge  
2. Who is affected (cohort table)  
3. Proposed changes (before → after from game definition)  
4. Data vs community alignment  
5. Risks (cards with evidence)  
6. Suggested solution paths  
7. Validation plan  
8. Draft player comms (collapsible)

> **Full UI spec:** see `ui-screen-list.md`

---

## 10. API Requirements

### 10.1 Core Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/health` | Health check |
| POST | `/api/analyze` | Run full pipeline |
| GET | `/api/analyze/demo` | Return fixture report |
| POST | `/api/mapping/suggest` | AI mapping suggestions |
| POST | `/api/mapping/confirm` | Save adapter |
| GET | `/api/mapping` | List adapters |

> **Full API spec:** see `api-list.md`

### 10.2 Swagger

- Available at `/swagger` on local and Azure deployment
- All endpoints documented for team testing without frontend

---

## 11. Non-Functional Requirements

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-1 | Performance | Analyze completes in &lt; 60 seconds (excluding LLM cold start) |
| NFR-2 | Reliability | Report generates via template if LLM fails |
| NFR-3 | Determinism | L0–L6: same input → same output |
| NFR-4 | Security | API keys in env vars only — never in git |
| NFR-5 | Usability | Demo mode loads all fixtures in one click |
| NFR-6 | Maintainability | Layers separated as C# services |
| NFR-7 | Compatibility | Modern Chrome / Edge desktop |
| NFR-8 | Scalability | MVP: single-user, no auth — acceptable |

---

## 12. Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React + Vite + TypeScript |
| Backend | ASP.NET Core 8 Web API |
| API docs | Swashbuckle (Swagger) |
| Backend host | Azure App Service |
| Frontend host | Vercel |
| LLM | OpenAI `gpt-4o-mini` or Gemini API |
| Storage (MVP) | In-memory / JSON files |

> **Full stack doc:** see `tech-stack.md`

---

## 13. Scope — In & Out

### 13.1 In Scope (Hackathon MVP)

- File upload analyze flow  
- 1 golden demo case + optional second case  
- Adapter mapping wizard (AI suggest + user fix)  
- L5/L6 rule patterns (minimum 3 each)  
- LLM report with template fallback  
- Swagger + deploy to Azure  

### 13.2 Out of Scope (Post-hackathon)

- Unity plugin / one-click export  
- Real-time telemetry streaming  
- User accounts and project history  
- PDF export  
- Multi-patch comparison  
- Full Reddit scraper production pipeline  

---

## 14. User Flows

### 14.1 First-Time Studio

```
Home → Mapping Setup (upload sample → AI suggest → fix → save adapter)
     → New Analysis (upload 6 files)
     → Processing
     → Report
```

### 14.2 Returning / Demo

```
Home → New Analysis (select saved adapter or demo)
     → Load demo data
     → Processing
     → Report
```

### 14.3 API-Only (Developer)

```
Swagger → POST /api/analyze (attach fixture files) → JSON report
```

---

## 15. Comparison with Existing Tools

| Dimension | Machinations | GameAnalytics | Patchframe |
|-----------|--------------|---------------|------------|
| **When** | Pre-launch design | Post-launch measure | Pre-ship patch decision |
| **Data** | Simulated model | Live telemetry | Telemetry + playtest + context |
| **Community** | No | No | Yes |
| **Output** | Simulation charts | Dashboards | Decision brief + risks |
| **Auto-balance** | No | No | No |
| **Relationship** | Complement | Data source | Decision layer on top |

---

## 16. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| AI hallucinates stats | LLM only on L7; evidence from L5/L6 JSON |
| Wrong adapter mapping | User review + confidence UI + preview |
| LLM API down at demo | Template fallback report |
| Scope creep in 24h | P0/P1/P2 priority; cut list in team plan |
| Entity ID mismatch | L4 validation with clear error messages |
| Judge asks "why not GA?" | Position as decision layer, not analytics replacement |

---

## 17. Success Criteria

### 17.1 Hackathon Demo

- [ ] Upload demo files → report in &lt; 60 seconds  
- [ ] Report shows who affected, risks, solution paths  
- [ ] At least one alignment pattern detected (e.g. perception vs data)  
- [ ] Evidence cited on every risk  
- [ ] Swagger works on Azure URL  
- [ ] Pitch clearly states "designer decides, not auto-balance"  

### 17.2 Product Vision (Post-hackathon)

- Studio can onboard new game with adapter setup in &lt; 30 minutes  
- Reduces patch meeting prep from hours to minutes  
- Integrates with GameAnalytics export as input  

---

## 18. Related Documents

| Document | Description |
|----------|-------------|
| `Architect.png` | System architecture diagram |
| `Workflow.png` | Runtime user workflow |
| `pipeline-layers-explained.md` | Layer-by-layer explanation + examples |
| `api-list.md` | Backend API specification |
| `ui-screen-list.md` | Frontend screen list |
| `tech-stack.md` | Technology choices |
| `game-definition-context-layer.md` | Game definition data spec |
| `ai-layers-setup-guide.md` | L5/L6/L7 implementation guide |
| `24h-team-plan.md` | 24-hour team execution plan |

---

## Appendix A — Glossary

| Term | Definition |
|------|------------|
| **Adapter** | Config mapping studio log format → canonical schema |
| **Canonical schema** | Standard event/field format defined by Patchframe |
| **Bracket** | Skill tier (e.g. low elo / high elo) |
| **Context bundle** | Merged game definition, rules, plan, community |
| **Solution path** | Type of fix (e.g. comms_only, targeted_by_bracket) — not a specific patch |
| **Alignment** | Agreement or conflict between data, community, and plan |
| **Design snapshot** | `game_definition.json` — current roster and stats |

---

## Appendix B — Example Scenario

**Game:** MOBA · **Entity:** Ironclad (char_A, tank)  
**Community:** "feels weak" (340 mentions)  
**Data:** 58% WR low bracket, 49% WR high bracket  
**Update plan:** Damage nerf 45 → 40  

**Patchframe output:**

- Pattern: perception vs data divergence + bracket split  
- Risk: stakeholder conflict (community wants buff; data shows strong in casual)  
- Suggested path: comms first or kit review — **not** blind nerf  
- Designer makes final call  

---

*End of Document*
