# Patch Brief

Decision-support tool for game designers. Upload telemetry, game definition, design rules, an update plan, and community context; the pipeline returns an **impact brief** — who is affected, where data and community diverge, what risks exist, and which kinds of solutions fit.

Patch Brief does **not** auto-balance games. It frames the patch so designers can decide before shipping.

#Demo Video

https://github.com/user-attachments/assets/b7b0e133-9919-49d0-b801-c6dfaa07ece9

## Architecture

Layers 0–6 are deterministic C#. Layer 7 writes the report (LLM if configured, otherwise a template fallback).

```
Upload → L0 Adapter → L1 Ingest → L2 Semantic → L3 Metric → L4 Context
       → L5 Impact → L6 Risk → L7 Report → Decision brief
```

| Layer | Role |
| ----- | ---- |
| L0 | Map studio field names to the canonical schema via `adapter.json` |
| L1 | Parse and validate telemetry events |
| L2 | Segment players into brackets and flag behavior patterns |
| L3 | Compute win / pick / death rates by bracket and entity |
| L4 | Merge game definition, rules, update plan, and community into a context bundle |
| L5 | Alignment between metrics, plan, and community |
| L6 | Risk types and solution-path templates |
| L7 | Structured report (JSON + markdown) |

## Repository layout

| Path | Contents |
| ---- | -------- |
| `backend/` | ASP.NET Core API (`GameBalance.Api`) and pipeline (`GameBalance.Pipeline`) |
| `frontend/` | React + Vite + TypeScript UI |
| `fixtures/demo_case/` | Golden demo inputs (Ironclad / Vex balance narrative) |
| `fixtures/l4_context_case/` | Extra L4 context fixture |
| `schemas/` | JSON data contracts |
| `.env.example` | Environment variables (no secrets) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (npm)

## Run locally

Copy environment defaults (optional; LLM and Apify are not required for a basic demo):

```powershell
Copy-Item .env.example .env
```

### Backend

```powershell
dotnet run --project backend\src\GameBalance.Api
```

API listens on **http://localhost:5278** (HTTPS: https://localhost:7253).  
Swagger UI: http://localhost:5278/swagger

Health check: `GET /api/analyze/health`

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

UI: **http://localhost:5173**

Vite proxies `/api` to the backend. Leave `VITE_API_URL` unset unless the API is on a different host.

### Demo

1. Start backend and frontend.
2. Open the app and run the built-in demo, or `POST /api/analyze/demo`.
3. Or upload files from `fixtures/demo_case/` (see that folder’s README for the balance story).

## Tests

```powershell
dotnet test backend\GameBalance.slnx
```

Frontend type-check and production build:

```powershell
cd frontend
npx tsc --noEmit
npm run build
```

## Environment

See `.env.example`. ASP.NET Core maps `__` to nested keys.

| Variable | Purpose |
| -------- | ------- |
| `Apify__ApiToken` | Optional Steam review scrape via Apify |
| `Apify__ActorId` | Apify actor id (default in `.env.example`) |
| `Llm__ApiKey` | Optional LLM for L7; empty uses the template report |
| `Llm__TimeoutSeconds` | LLM call timeout (default 60) |

Never commit tokens. Steam scrape details: `backend/README.md`.

## More documentation

- [Pipeline layers](pipeline-layers-explained.md)
- [AI layers (L5–L7)](ai-layers-setup-guide.md)
- [SRS](SRS-Patchframe.md)
- [Backend](backend/README.md)
- [Frontend](frontend/README.md)
- [Demo fixture](fixtures/demo_case/README.md)
