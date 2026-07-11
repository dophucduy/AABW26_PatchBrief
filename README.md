# Game Balance / Patch Decision Brief Tool

A multi-module pipeline that ingests player telemetry, game definitions, community sentiment, and patch plans, then produces an AI-powered patch decision brief for game designers.

## Architecture

```
WEB (deterministic)                          AI (narrative)
L0 Adaptive → L1 Ingest → L2 Semantic → L3 Metric → L4 Context → L5 Impact → L6 Risk → L7 Report → User
```

| Layer | Module | Responsibility |
|-------|--------|----------------|
| L0 | `l0_adaptive` | Map studio field names to canonical schema via adapter.json |
| L1 | `l1_ingest` | Parse, validate, tag telemetry events |
| L2 | `l2_semantic` | Segment brackets, behavior profiles, pattern flags |
| L3 | `l3_metric` | Win/pick/death rates by bracket + entity |
| L4 | `l4_context` | Merge game_definition + rules + update_plan + community |
| L5 | `l5_impact` | AI impact & alignment analysis |
| L6 | `l6_risk` | AI risk & solution framing |
| L7 | `l7_report` | Final insight report (JSON + markdown) |

## Repo layout

```
backend/            FastAPI app + layer modules
frontend/           React + Vite upload UI and report view
schemas/            JSON schema definitions for all data contracts
fixtures/           Golden demo cases
```

## Run

Backend:
```
cd backend
pip install -r requirements.txt
cp .env.example .env      # add your LLM API key
uvicorn app.main:app --reload
```

Frontend:
```
cd frontend
npm install
npm run dev
```

## Demo

Upload the files in `fixtures/demo_case/` through the frontend or via:
```
curl -F player_online=@fixtures/demo_case/player_online.json \
     -F player_offline=@fixtures/demo_case/player_offline.json \
     -F game_definition=@fixtures/demo_case/game_definition.json \
     -F rules=@fixtures/demo_case/rules.json \
     -F update_plan=@fixtures/demo_case/update_plan.json \
     -F community=@fixtures/demo_case/community.json \
     -F adapter=@fixtures/demo_case/adapter.json \
     http://localhost:8000/analyze
```

## Deployment

- Backend → Railway (`backend/railway.json`)
- Frontend → Vercel (`frontend/vercel.json`)
