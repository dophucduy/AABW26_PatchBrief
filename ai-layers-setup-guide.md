# AI Layers Setup Guide (L5, L6, L7)

**Owner:** Member C — AI Engine  
**Stack:** C# in `PatchBrief.Core` · LLM via **cloud API** (not local)

---

## Important: What "AI Part" Actually Means

| Layer | Name | Needs AI model? | What you build |
|-------|------|-----------------|----------------|
| **L5** | Impact & Alignment | **No** | C# rule engine |
| **L6** | Risk & Solution Framing | **No** | C# rule engine |
| **L7** | Insight + Report | **Yes** | 1 LLM API call |

**~90% of your work is C# rules.** LLM is only the final report writer.

You do **NOT** need to:
- Train a model
- Download weights
- Run Ollama / local GPU (optional only)

You **DO** need:
- OpenAI or Gemini **API key** (free tier / credits)
- `HttpClient` in C#
- Rule definitions (YAML or JSON)

---

## Do I Need to Run AI Local?

| Option | Need local AI? | Hackathon recommendation |
|--------|----------------|--------------------------|
| **OpenAI API** (`gpt-4o-mini`) | No | ✅ **Best choice** |
| **Gemini API** | No | ✅ Good alternative |
| **Azure OpenAI** | No | ✅ If team has Azure credits |
| **Ollama (Llama, Qwen)** | Yes — install Ollama | ⚠️ Only if no API key / offline demo |
| **Train / fine-tune** | Yes | ❌ Don't do this |

**Answer: No, run LLM in the cloud.** Your C# backend calls the API from Azure App Service.

---

## What You Receive (from other team members)

By Hour 10, you need these JSON shapes from L3 + L4:

### From L3 — `metrics.json`

```json
{
  "char_A": {
    "live": {
      "sessions": 500,
      "pick_rate_low": 0.22,
      "win_rate_low": 0.58,
      "win_rate_high": 0.49
    },
    "playtest": { "win_rate_all": 0.51 }
  }
}
```

### From L4 — `context_bundle.json`

```json
{
  "joined_changes": [
    {
      "entity_id": "char_A",
      "entity_name": "Ironclad",
      "role": "tank",
      "field": "base_damage",
      "from": 45,
      "to": 40,
      "lever_status": "open"
    }
  ],
  "community": {
    "clusters": [
      { "entity_id": "char_A", "theme": "feels_weak", "volume": 340 }
    ]
  }
}
```

**You do not parse raw logs.** Wait for metrics + context bundle.

---

## What You Produce

### L5 output → `impact.json`

```json
{
  "who_is_affected": [],
  "alignment": {
    "data_vs_community": "divergent",
    "patterns": []
  }
}
```

### L6 output → `risks.json`

```json
{
  "risks": [],
  "solution_paths": [],
  "validation_plan": []
}
```

### L7 output → `AnalyzeResponse` (full report)

Merge L5 + L6 + LLM narrative → see `api-list.md`

---

## Project Setup

### 1. Folder structure in `PatchBrief.Core`

```
PatchBrief.Core/
  Models/
    MetricsSnapshot.cs
    ContextBundle.cs
    ImpactResult.cs
    RiskResult.cs
    AnalyzeResponse.cs
  Services/
    ImpactService.cs          ← L5
    RiskService.cs            ← L6
    LlmReportService.cs       ← L7
  Rules/
    impact_patterns.json      ← L5 rules
    risk_patterns.json        ← L6 rules
  Prompts/
    report_system.txt         ← L7 system prompt
```

### 2. Register services in `Program.cs`

```csharp
builder.Services.AddHttpClient<LlmReportService>();
builder.Services.AddScoped<ImpactService>();
builder.Services.AddScoped<RiskService>();
builder.Services.AddScoped<LlmReportService>();
```

### 3. API key (local dev)

**`appsettings.Development.json`** (do NOT commit real key):

```json
{
  "Llm": {
    "Provider": "OpenAI",
    "ApiKey": "sk-...",
    "Model": "gpt-4o-mini",
    "BaseUrl": "https://api.openai.com/v1"
  }
}
```

**Azure App Service:** Configuration → Application settings → `Llm__ApiKey`

### 4. User secrets (local — recommended)

```bash
cd PatchBrief.Api
dotnet user-secrets init
dotnet user-secrets set "Llm:ApiKey" "your-key-here"
```

---

## L5 — ImpactService (No LLM)

### Responsibility

- Read `metrics` + `context_bundle`
- Run pattern rules
- Output `who_is_affected` + `alignment.patterns`

### Pattern rules example (`Rules/impact_patterns.json`)

```json
[
  {
    "id": "bracket_split_easy_low",
    "condition": "win_rate_low > win_rate_high + 0.08",
    "description": "Strong in low bracket, weaker in high"
  },
  {
    "id": "perception_vs_data_divergence",
    "condition": "community_theme_feels_weak AND win_rate_near_average",
    "description": "Players say weak; data shows average performance"
  },
  {
    "id": "plan_conflicts_with_data",
    "condition": "update_plan_nerfs AND win_rate_low > 0.55",
    "description": "Nerfing entity already strong in casual play"
  }
]
```

### Implement as simple C# if/else first

```csharp
public class ImpactService
{
    public ImpactResult Analyze(MetricsSnapshot metrics, ContextBundle context)
    {
        var result = new ImpactResult();

        foreach (var change in context.JoinedChanges)
        {
            var m = metrics.GetEntity(change.EntityId);
            if (m == null) continue;

            // Pattern: bracket split
            if (m.Live.WinRateLow > m.Live.WinRateHigh + 0.08)
            {
                result.Patterns.Add(new Pattern
                {
                    Id = "bracket_split_easy_low",
                    EntityId = change.EntityId,
                    Confidence = "high",
                    Evidence = new[] { $"wr_low: {m.Live.WinRateLow}", $"wr_high: {m.Live.WinRateHigh}" }
                });
            }

            // Who is affected
            result.WhoIsAffected.Add(new AffectedCohort
            {
                EntityId = change.EntityId,
                EntityName = change.EntityName,
                Cohort = "low_bracket",
                Impact = m.Live.WinRateLow > 0.55 ? "high" : "medium",
                Reason = $"Patch changes {change.Field} {change.Delta}"
            });
        }

        // Alignment: community vs data
        result.Alignment.DataVsCommunity = DetectDivergence(metrics, context);

        return result;
    }
}
```

**Start with 3–4 patterns.** Add more if time allows.

---

## L6 — RiskService (No LLM)

### Responsibility

- Read L5 `ImpactResult` + `context_bundle`
- Map to risk types + solution paths
- Output validation plan

### Risk rules (hardcode first, extract to JSON later)

| If pattern / condition | Risk | Solution path |
|------------------------|------|---------------|
| `perception_vs_data_divergence` | `comms_backlash` | `comms_only` |
| `bracket_split_easy_low` + plan buffs | `stakeholder_conflict` | `targeted_by_bracket` |
| `lever_status == locked` | `identity_lever_conflict` | `tune_numbers` blocked |
| Tank role + damage nerf | `second_order_meta` | `iterate_playtest` |

```csharp
public class RiskService
{
    public RiskResult Analyze(ImpactResult impact, ContextBundle context)
    {
        var result = new RiskResult();

        foreach (var pattern in impact.Patterns)
        {
            if (pattern.Id == "perception_vs_data_divergence")
            {
                result.Risks.Add(new Risk
                {
                    Id = "stakeholder_conflict",
                    Severity = "high",
                    Title = "Community wants buff; data does not support it",
                    Evidence = pattern.Evidence.ToList()
                });
                result.SolutionPaths.Add(new SolutionPath
                {
                    Type = "comms_only",
                    Confidence = "medium",
                    Rationale = "Address perception before numeric change",
                    DesignerDecides = true
                });
            }
        }

        result.ValidationPlan.Add("Survey: fun to play vs feels weak");
        return result;
    }
}
```

---

## L7 — LlmReportService (Only LLM Layer)

### Responsibility

- Input: `metrics` + `impact` + `risks` (all structured JSON)
- Call OpenAI / Gemini **once**
- Output: `report_markdown` + `executive_summary` + `draft_player_comms`

### Flow

```
ImpactResult + RiskResult + MetricsSnapshot + ContextBundle
    → serialize to JSON string
    → POST to LLM with system prompt
    → parse response (JSON mode recommended)
    → AnalyzeResponse
```

### System prompt rules

Tell the LLM:

1. Only use data provided in the JSON
2. Do not invent stats or quotes
3. Every claim must reference evidence from input
4. Suggest paths as "investigate / designer decides" — not "nerf by X%"
5. If `intentional_difficulty` or locked lever — note it

### Fallback (required for demo)

If LLM fails (no key, timeout, rate limit):

```csharp
public async Task<AnalyzeResponse> GenerateReportAsync(...)
{
    try
    {
        return await CallLlmAsync(bundle);
    }
    catch
    {
        return TemplateReportBuilder.Build(impact, risks, context);
    }
}
```

**Template fallback** = string format from L5/L6 JSON. Demo still works without internet.

---

## LLM Provider Setup

### Option A — OpenAI (recommended)

| Setting | Value |
|---------|-------|
| URL | `https://api.openai.com/v1/chat/completions` |
| Model | `gpt-4o-mini` (cheap, fast) |
| Key | https://platform.openai.com/api-keys |

Headers: `Authorization: Bearer {apiKey}`

Use `response_format: { "type": "json_object" }` for structured output.

### Option B — Google Gemini

| Setting | Value |
|---------|-------|
| URL | `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent` |
| Key | Google AI Studio |

### Option C — Azure OpenAI

Same as OpenAI but different `BaseUrl` + Azure key. Good if hackathon sponsors Azure.

### Option D — Local Ollama (optional)

Only if you cannot use cloud API:

1. Install Ollama
2. `ollama pull llama3.2` or `qwen2.5`
3. BaseUrl = `http://localhost:11434/v1`
4. **Problem:** Azure backend cannot reach your laptop's Ollama — only works for local dev

---

## Development Order (Your 24h)

| Hour | Task |
|------|------|
| 2–4 | Create models + read `fixtures/metrics.json` + `context_bundle.json` |
| 4–6 | `ImpactService` — 3 patterns working |
| 6–8 | `RiskService` — 3 risks + 3 solution paths |
| 8–10 | Unit test L5+L6 with fixtures → `insights.json` output |
| 10–12 | `LlmReportService` + API key + prompt |
| 12–14 | Template fallback + wire into `POST /api/analyze` |
| 14–16 | Test full pipeline with Member E |

**Do L5 + L6 before touching LLM.**

---

## How to Test Without Other Team Members

Use fixtures in `fixtures/demo_case/`:

```
metrics.json          ← write mock yourself if L3 not ready
context_bundle.json   ← write mock yourself if L4 not ready
expected_impact.json  ← your expected L5 output (optional)
```

**Console test project:**

```bash
dotnet run --project PatchBrief.Core.Tests
```

Or temporary endpoint:

```
GET /api/analyze/demo  → runs L5+L6+L7 on fixtures
```

---

## Mapping API (stretch — if you have time)

`POST /api/mapping/suggest` also uses LLM:

- Input: sample user JSON + canonical schema list
- Output: suggested `field_map` + `event_map` with confidence

Same `LlmReportService` or separate `MappingSuggestService` with different prompt.

**Priority:** L7 report first. Mapping suggest is P1 stretch.

---

## Checklist Before Demo

- [ ] L5 runs without LLM
- [ ] L6 runs without LLM
- [ ] L7 works with API key on Azure
- [ ] L7 fallback works without API key
- [ ] Every risk has `evidence[]`
- [ ] Every solution has `designer_decides: true`
- [ ] Swagger `POST /api/analyze` returns full report
- [ ] API key in Azure App Settings (not in git)

---

## Common Mistakes to Avoid

| Mistake | Why bad |
|---------|---------|
| Put LLM in L5/L6 | Non-deterministic, can't debug, hallucinated patterns |
| Send raw logs to LLM | Token overflow + invented metrics |
| No fallback | Demo dies if API fails |
| Commit API key | Security + judge trust |
| Wait for full pipeline before starting | Use fixtures on hour 2 |

---

## One-Line Summary

> **You build two C# rule engines (L5, L6) and one API call for the report (L7). No local AI required — use OpenAI/Gemini from Azure.**
