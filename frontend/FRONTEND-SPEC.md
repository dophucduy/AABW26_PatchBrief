# Patch Brief — Frontend Specification

**Version:** 0.1 MVP  
**Owner:** Frontend  
**Stack:** React + Vite + TypeScript  
**Backend contract:** ASP.NET Core API described in `api-list.md`  
**UI flow:** Described in `ui-screen-list.md`

---

## 1. Product intent

Patch Brief helps game designers frame balance decisions before shipping a patch. The frontend collects game evidence, optionally maps a studio's custom event format, runs the analysis pipeline, and presents a decision-ready impact brief.

The frontend must make three things clear:

1. What evidence is being analyzed.
2. Which players, signals, and risks are affected.
3. Which solution path remains the designer's decision.

The frontend does not auto-apply patches and must not imply that the suggested solution is an automatic recommendation.

---

## 2. Scope and boundaries

### In scope

- React UI and client-side routing.
- JSON file upload and client-side validation.
- Mapping wizard for Layer 0 adapter setup.
- Analyze upload flow.
- Processing state and progress feedback.
- Report rendering from API response or demo data.
- API client calls defined in `src/api.ts`.
- Local/session browser storage used for the current demo flow.
- Responsive layout, focus states, and reduced-motion support.

### Out of scope

- Backend implementation or API behavior changes.
- Database, authentication, authorization, and user accounts.
- Automatic patch application.
- Project/history management, PDF export, compare plans, and game-definition editing unless added as a future feature.

Only files inside `frontend/` should be changed for frontend work.

---

## 3. User flow and routes

```text
/                         Home
/mapping                  Mapping — Upload sample
/mapping/review           Mapping — Review suggestions
/mapping/confirm          Mapping — Save adapter
/analyze                  New analysis / upload evidence
/analyze/loading          Processing pipeline
/report                   Impact report
/error                    Error fallback state
```

### Primary flows

```text
First-time studio:
Home → Mapping upload → Mapping review → Mapping confirm → Analyze → Loading → Report

Returning studio:
Home → Analyze → Loading → Report

Demo:
Home → Analyze → Load demo data → Loading → Report
```

### Navigation rules

- The `PATCH/BRIEF` brand always returns to `/`.
- The global header contains Analyze, Mapping setup, and Latest report.
- `/mapping/review` has a contextual Back action to `/mapping`.
- `/error` has a Back action to `/analyze`.
- `/analyze` has `Back to home`.
- `/mapping` has `Back to home`.
- `/mapping/confirm` has `Back to review`.
- `/report` has `Back to analyze`.
- `/analyze/loading` intentionally has no Back action while the pipeline is in progress. A future Cancel action must abort the request before navigating away.
- Route targets must be explicit. Do not rely only on `window.history.back()` because the user may have entered a route directly.

The shared route-level navigation is implemented by `RouteBack` in `src/main.tsx`.

---

## 4. Screen requirements

### 4.1 Home

**Purpose:** Explain the product and start a flow.

Required actions:

- Start an analysis → `/analyze`.
- Set up data mapping → `/mapping`.
- The global brand returns to Home.

Content should communicate:

- Evidence in: telemetry, playtests, rules, update plan, and community signal.
- Decision out: affected cohorts, proposed changes, risks, solution paths, and validation plan.

### 4.2 Mapping upload

**Purpose:** Upload a representative studio log and request mapping suggestions.

Required controls:

- Genre: `MOBA`, `FPS`, `RPG`, or `Other`.
- JSON file dropzone.
- Suggest mapping button, disabled until a valid JSON file is present.
- Back to Home.

Client validation:

- Accept `.json` and `application/json`.
- Show a clear error when the selected file is not JSON.
- Do not modify the user's source file.

### 4.3 Mapping review

**Purpose:** Let the user correct low-confidence mappings.

Required UI:

- Tabs for All, Field, and Event maps.
- Source field, sample value, canonical target, and confidence columns.
- Editable canonical target select.
- Confidence states:
  - High: `>= 90%`.
  - Medium: `70–89%`.
  - Low: `< 70%`.
- Live preview with parsed count, skipped count, warnings, and sample output.
- Back to Mapping upload.
- Continue to Confirm.

Canonical target values are defined in `src/data.ts` and should be replaced by `/api/schema/canonical` when that endpoint is enabled.

### 4.4 Mapping confirm

**Purpose:** Save a reusable adapter.

Required UI:

- Read-only final mapping summary.
- Adapter name input.
- Default-for-session checkbox.
- Back to Review.
- Save adapter action.

Adapter names should be normalized to lowercase letters, numbers, and underscores.

### 4.5 Analyze

**Purpose:** Collect all files needed for the pipeline.

Required files:

| Field | File | Required |
|---|---|---|
| `player_online` | `player_online.json` | Yes |
| `player_offline` | `player_offline.json` | Yes |
| `game_definition` | `game_definition.json` | Yes |
| `rules` | `rules.json` | Yes |
| `update_plan` | `update_plan.json` | Yes |
| `community` | `community.json` | No |

Required controls:

- Adapter select.
- Create new mapping link.
- Per-file upload state: Missing or Ready.
- Load demo data.
- Run analysis, disabled until all five required files are present.
- Back to Home.

### 4.6 Processing

Show progress feedback for:

1. Ingest and normalize.
2. Metrics and cohorts.
3. Context merge.
4. Impact and risk.
5. Generating brief.

The current MVP uses simulated progress in demo mode and waits at least 5.6 seconds before navigating to the report. With the API enabled, navigation waits for the report response and routes to the error state after 60 seconds without a result. When the backend returns a job-based response in the future, this screen should poll the job endpoint instead of using the current response-wait logic.

### 4.7 Report

The report is the core demo screen and should be readable as one scrollable brief.

Required sections:

1. Executive summary.
2. Overall exposure/risk.
3. Who is affected.
4. Proposed changes.
5. Data vs community alignment.
6. Risks and evidence.
7. Suggested solution paths, labeled `Designer decides`.
8. Validation plan.
9. Draft player communications.
10. New analysis action.

The report must not present solution paths as automatic patch commands.

### 4.8 Error state

Errors must explain what happened and what the user can do next.

Supported examples:

- Missing required files.
- Invalid JSON extension.
- API validation error.
- Entity ID mismatch.
- API 500 or network failure.
- LLM fallback with a visible template-fallback notice.

Avoid vague messages such as `Something went wrong` without a next action.

---

## 5. API integration

API base URL is configured with:

```env
VITE_API_URL=https://your-api-host
```

When `VITE_API_URL` is absent, the app runs in local demo mode.

### Client methods

`src/api.ts` exposes:

| Client method | Endpoint | Used by |
|---|---|---|
| `getHealth()` | `GET /api/analyze/health` | App boot status |
| `suggestMapping(file, genre)` | `POST /api/mapping/suggest` | Mapping upload |
| `previewMapping(file, fieldMap, eventMap)` | `POST /api/mapping/preview` | Mapping review |
| `confirmMapping(payload)` | `POST /api/mapping/confirm` | Mapping confirm |
| `listAdapters()` | `GET /api/mapping` | Analyze adapter select |
| `runAnalysis(files, adapterId)` | `POST /api/analyze` | Analyze submit |

### Multipart rules

Analyze and mapping upload requests use `FormData`. Do not manually set the `Content-Type` header for multipart requests; the browser must add the boundary.

The analyze request must append these backend field names:

```text
playerOnline
playerOffline
gameDefinition
rules
updatePlan
community (optional)
adapterId or adapter
```

The UI keeps snake_case keys for local file state (`player_online`, `game_definition`, and so on), and `src/api.ts` translates them to the backend's camelCase multipart names.

### Error handling

The client should read the standard backend shape:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Human-readable message",
    "details": []
  }
}
```

The UI should prefer `error.message`, then provide a route-specific recovery action.

---

## 6. Client-side state

### React state

Use local component state for temporary input state:

- Selected files.
- Current mapping tab and edits.
- Selected adapter.
- Loading step.
- Inline error or success state.

### Session storage

Current keys:

| Key | Value | Purpose |
|---|---|---|
| `patchBriefReport` | Serialized report JSON | Pass report from Loading to Report |
| `patchBriefError` | Error message | Preserve API fallback context |
| `mappingRows` | Serialized mapping rows | Pass mapping edits between wizard screens |
| `mappingFile` | Original filename | Display mapping source |
| `patchBriefStartedAt` | Timestamp string | Coordinate the Loading wait/timeout state |

### Local storage

| Key | Value | Purpose |
|---|---|---|
| `patchBriefDefaultAdapter` | Adapter ID | Remember the user's default adapter for the browser session/demo |

Do not put raw file contents in localStorage or sessionStorage unless a future requirement explicitly allows it.

---

## 7. Design system

The visual direction is a balance-lab / patch-briefing workspace: dark telemetry canvas, paper-like report surface, and clear signal colors.

### Color tokens

| Token | Value | Use |
|---|---|---|
| `--ink` | `#111817` | Main application background |
| `--ink-2` | `#192321` | Cards and panels |
| `--paper` | `#ebe9df` | Report and form surfaces |
| `--mint` | `#b6f2c1` | Positive signal, active state, primary CTA |
| `--coral` | `#f07866` | Risk, warnings, key accent |
| `--yellow` | `#e5c875` | Medium confidence/risk |

### Typography

- Display: Space Grotesk with a system fallback.
- Body: Manrope with a system fallback.
- Utility/data: DM Mono with a monospace fallback.

Typography should remain sentence case for user-facing copy. Use uppercase only for short utility labels, statuses, and metadata.

### Interaction and motion

- Use one primary action per screen.
- Use explicit action labels such as `Save adapter`, `Run analysis`, and `Back to review`.
- Keep hover and progress motion subtle.
- Respect `prefers-reduced-motion: reduce`.
- Maintain visible keyboard focus.
- Avoid adding decorative animation that does not clarify state or hierarchy.

---

## 8. Component conventions

Shared building blocks currently live in `src/main.tsx`. As the app grows, move them into `src/components/` without changing behavior.

Reusable components:

- `AppShell` — global header, main area, footer, API status.
- `RouteBack` — explicit route-level Back action.
- `Button` — primary and quiet actions.
- `Eyebrow` — small context label.
- `FileRow` — JSON upload status row.
- `StepIndicator` — Mapping wizard progress.
- `ConfidenceBadge` — mapping confidence state.
- `RiskBadge` — report risk state.

Suggested future structure:

```text
frontend/
  src/
    api.ts
    data.ts
    main.tsx
    styles.css
    components/
      AppShell.tsx
      Button.tsx
      FileDropzone.tsx
      RouteBack.tsx
      StepIndicator.tsx
    pages/
      Home.tsx
      MappingUpload.tsx
      MappingReview.tsx
      MappingConfirm.tsx
      Analyze.tsx
      Loading.tsx
      Report.tsx
      ErrorPage.tsx
```

Do not split files only for cosmetic reasons. Extract a component when it is reused, has its own state/behavior, or can be tested independently.

---

## 9. Extension rules

When adding a new route:

1. Add the route to the route map in `App`.
2. Decide whether it needs a contextual Back action.
3. Add the route to the global header only if it is a primary destination.
4. Keep API calls in `src/api.ts`, not inside repeated UI primitives.
5. Add loading, empty, error, and success states.
6. Add mobile behavior and keyboard focus behavior.
7. Use existing color, type, and spacing tokens before adding new ones.
8. Run a production build before handoff.

When changing a backend contract:

- Update the frontend API client and this spec together.
- Do not modify backend files from the frontend task.
- Keep demo fallback data valid so the UI remains usable without the API.

---

## 10. MVP acceptance checklist

- [ ] Home can start an analysis.
- [ ] Home can start Mapping setup.
- [ ] Mapping upload accepts a JSON file and rejects non-JSON files.
- [ ] Mapping review allows target edits.
- [ ] Mapping confirm saves an adapter locally and can call the API.
- [ ] Analyze displays all six file rows.
- [ ] Analyze disables submit until five required files are ready.
- [ ] Demo data fills the upload state.
- [ ] Loading shows pipeline progress.
- [ ] Report renders from demo data without a backend.
- [ ] Report displays risks, changes, solution paths, and validation plan.
- [ ] Back actions use explicit route targets.
- [ ] Global brand returns Home.
- [ ] Focus states are visible.
- [ ] Reduced motion is respected.
- [ ] TypeScript has no errors with `npx tsc --noEmit`.
- [ ] `npm.cmd run build` passes.

---

## 11. Local development

```powershell
cd D:\hackathon\AABW26_ChickenLavender\frontend
npm.cmd install
npm.cmd run dev
```

Production verification:

```powershell
npm.cmd run build
npm.cmd run preview
```

The current implementation is intentionally demo-safe: with no `VITE_API_URL`, it uses the report and mapping fixtures in `src/data.ts` while preserving the API integration points for backend wiring.
