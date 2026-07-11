# Requirements Document

## Introduction

The Game Balance / Patch Decision Brief Tool is a multi-module pipeline system that ingests player telemetry, game definitions, community sentiment, and patch plans, then produces AI-powered insight reports for game designers. The system follows a layered architecture (L0–L7) where deterministic data processing (L1–L4) feeds into AI-driven analysis (L5–L7). The pipeline accepts structured JSON inputs via a REST API and outputs a comprehensive decision brief including executive summary, affected cohorts, risk analysis, and solution paths. The project is designed for parallel development by a 5-person team during a 24h hackathon and deployed as a private GitHub repository with modular folder structure.

## Glossary

- **Pipeline**: The end-to-end processing system from file upload to report generation
- **Layer**: A discrete processing stage in the pipeline (L0 through L7)
- **Adaptive_Layer (L0)**: Module that maps studio-specific field names to canonical schema using adapter.json
- **Ingest_Normalizer (L1)**: Module that parses, validates, and tags raw telemetry files
- **Semantic_Analyzer (L2)**: Module that segments players into brackets, builds behavior profiles, and flags patterns
- **Metric_Engine (L3)**: Module that computes win rates, pick rates, death rates by bracket and entity
- **Context_Layer (L4)**: Module that merges game_definition, rules, update_plan, and community into context_bundle
- **Impact_Analyzer (L5)**: AI module that evaluates alignment between metrics, plan, and community
- **Risk_Framer (L6)**: AI module that identifies risks and generates solution paths
- **Report_Generator (L7)**: Module that produces the final insight report as structured JSON and markdown
- **Entity**: Any game object (character, weapon, item) defined in game_definition.json
- **Bracket**: A skill tier or ELO range used for cohort segmentation (e.g., bronze, diamond)
- **Context_Bundle**: The merged output of L4 combining game definition, rules, update plan, and community data
- **Telemetry**: Player behavior data collected from live or playtest sessions
- **Decision_Brief**: The final output report containing executive summary, risks, solution paths, and validation plan
- **Adapter**: A mapping configuration (adapter.json) that translates studio-specific field names to the canonical pipeline schema
- **Golden_Case**: A reference demo fixture set used for validation and testing

## Requirements

### Requirement 1: Repository Structure and Project Setup

**User Story:** As a hackathon team member, I want a modular repository structure with separate folders per pipeline layer, so that 5 people can develop in parallel without merge conflicts.

#### Acceptance Criteria

1. THE Repository SHALL organize source code into separate directories for each pipeline layer (L0 through L7)
2. THE Repository SHALL include a `backend/` root containing the FastAPI application and layer modules
3. THE Repository SHALL include a `frontend/` root containing the React+Vite application
4. THE Repository SHALL include a `schemas/` directory with JSON schema definitions for all data contracts
5. THE Repository SHALL include a `fixtures/` directory with at least one golden demo case containing all required input files
6. THE Repository SHALL include a `.env.example` file documenting required environment variables without secret values
7. THE Repository SHALL include a README with instructions to run the backend, frontend, and demo fixtures

### Requirement 2: API Endpoint and File Upload

**User Story:** As a game designer, I want to upload my data files via a single API call, so that I can receive a complete patch decision brief without manual processing steps.

#### Acceptance Criteria

1. THE Pipeline SHALL expose a `POST /analyze` endpoint accepting multipart/form-data with files: player_online, player_offline, game_definition, rules, update_plan, community, and adapter
2. WHEN all required files are uploaded, THE Pipeline SHALL return a JSON response containing: metrics, insights, risks, solutions, report_markdown
3. WHEN a required file is missing from the upload, THE Pipeline SHALL return a descriptive error indicating which file is absent
4. WHEN an uploaded file contains invalid JSON, THE Pipeline SHALL return an error identifying the malformed file and parse failure reason
5. THE Pipeline SHALL process a complete upload and return a report within 60 seconds under normal conditions

### Requirement 3: Adaptive Layer (L0)

**User Story:** As a game studio with custom telemetry formats, I want the pipeline to map my field names to a canonical schema, so that I do not need to restructure my existing data exports.

#### Acceptance Criteria

1. WHEN adapter.json is provided, THE Adaptive_Layer SHALL map studio-specific field names in telemetry files to the canonical pipeline schema
2. WHEN adapter.json is not provided, THE Adaptive_Layer SHALL assume input files already conform to canonical schema
3. IF adapter.json references a field that does not exist in the uploaded telemetry, THEN THE Adaptive_Layer SHALL log a warning and skip that mapping without failing the pipeline
4. THE Adaptive_Layer SHALL pass through fields not mentioned in adapter.json without modification

### Requirement 4: Ingest and Normalize (L1)

**User Story:** As a pipeline developer, I want raw telemetry parsed and validated into a normalized format, so that downstream layers receive clean, consistent event data.

#### Acceptance Criteria

1. WHEN player_online.json is uploaded, THE Ingest_Normalizer SHALL parse events and tag each with `source: "online"`
2. WHEN player_offline.json is uploaded, THE Ingest_Normalizer SHALL parse events and tag each with `source: "offline"`
3. THE Ingest_Normalizer SHALL validate that each event contains a recognized `event_type` from the supported set: session_start, match_end, death, ability_used, entity_pick, area_enter
4. IF an event has an unrecognized event_type, THEN THE Ingest_Normalizer SHALL discard the event and log a warning with the event index and type
5. THE Ingest_Normalizer SHALL validate that each event contains a timestamp and entity_id field
6. IF an event is missing required fields, THEN THE Ingest_Normalizer SHALL discard the event and include it in a validation error summary

### Requirement 5: Gameplay Semantic Analysis (L2)

**User Story:** As a game designer, I want players segmented into skill brackets with behavior profiles, so that I can understand how balance changes affect different skill levels differently.

#### Acceptance Criteria

1. WHEN normalized events are received from L1, THE Semantic_Analyzer SHALL segment players into bracket cohorts using bracket definitions from game_definition.json
2. THE Semantic_Analyzer SHALL generate behavior profiles per bracket including play patterns and entity preferences
3. THE Semantic_Analyzer SHALL flag at least three behavior patterns: bracket_split (entity performs differently across brackets), one_trick (player uses single entity disproportionately), and meta_dominant (entity picked in majority of matches at a bracket)
4. WHEN fewer than 5 events exist for a bracket-entity combination, THE Semantic_Analyzer SHALL mark that combination as low_confidence in the output

### Requirement 6: Metric and Cohort Engine (L3)

**User Story:** As a game designer, I want computed metrics like win rate, pick rate, and death rate broken down by bracket and entity, so that I have quantitative evidence for balance decisions.

#### Acceptance Criteria

1. THE Metric_Engine SHALL compute win_rate, pick_rate, and death_rate per entity per bracket from the normalized event data
2. THE Metric_Engine SHALL output a metrics.json structure with each metric keyed by entity_id and bracket_id
3. WHEN playtest (offline) and live (online) data are both present, THE Metric_Engine SHALL compute metrics separately for each source and include a comparison field
4. IF no match_end events exist for an entity-bracket combination, THEN THE Metric_Engine SHALL report win_rate as null rather than zero for that combination

### Requirement 7: Context Layer (L4)

**User Story:** As a game designer, I want my game definition, balance rules, update plan, and community sentiment merged into a single context bundle, so that the AI layers have full information to judge patch impact.

#### Acceptance Criteria

1. THE Context_Layer SHALL parse game_definition.json and extract entities with their id, name, type, role, tags, and stats
2. THE Context_Layer SHALL parse rules.json and associate locked/open levers with each entity
3. THE Context_Layer SHALL parse update_plan.json and resolve each change target to an entity in game_definition.json
4. IF update_plan.json references an entity_id not found in game_definition.json, THEN THE Context_Layer SHALL return a validation error identifying the unresolved reference
5. THE Context_Layer SHALL parse community.json and cluster sentiment entries by entity and theme
6. THE Context_Layer SHALL output a merged context_bundle.json containing game definition, rules, planned changes with resolved entity details, and community clusters
7. WHEN metrics.json contains an entity_id not present in game_definition.json, THE Context_Layer SHALL flag a warning indicating unresolved telemetry entity

### Requirement 8: Impact and Alignment Analysis (L5)

**User Story:** As a game designer, I want AI-driven analysis of how planned changes align or conflict with player data and community sentiment, so that I can identify potential problems before shipping a patch.

#### Acceptance Criteria

1. THE Impact_Analyzer SHALL evaluate the `bracket_split_easy_low` pattern: detecting when an entity is disproportionately strong in low brackets but average or weak in high brackets
2. THE Impact_Analyzer SHALL evaluate the `perception_vs_data_divergence` pattern: detecting when community sentiment about an entity diverges from actual metric data
3. THE Impact_Analyzer SHALL evaluate the `identity_lever_conflict` pattern: detecting when a planned change modifies a stat that defines the entity's role identity
4. THE Impact_Analyzer SHALL evaluate the `second_order_meta_risk` pattern: detecting when a change to one entity likely shifts pick/ban patterns for related entities
5. THE Impact_Analyzer SHALL produce an alignment assessment comparing data_vs_community and playtest_vs_live for each affected entity
6. WHEN the Impact_Analyzer identifies a pattern match, THE Impact_Analyzer SHALL include evidence references linking to specific metrics and context data that triggered the match

### Requirement 9: Risk and Solution Framing (L6)

**User Story:** As a game designer, I want identified risks paired with actionable solution paths, so that I can make informed patch decisions with clear trade-offs.

#### Acceptance Criteria

1. THE Risk_Framer SHALL classify identified risks with a severity level (low, medium, high) and a risk type identifier
2. THE Risk_Framer SHALL generate at least one solution path for each identified risk
3. THE Risk_Framer SHALL include a confidence level (low, medium, high) and rationale for each solution path
4. THE Risk_Framer SHALL support solution types including: targeted_by_bracket, communication_framing, iterative_change, and revert_recommendation
5. WHEN a risk involves a locked lever (from rules.json), THE Risk_Framer SHALL exclude solution paths that modify that lever and note the constraint
6. THE Risk_Framer SHALL attach evidence arrays to each risk citing the specific metrics and context entries that support the risk assessment

### Requirement 10: Insight Report Generation (L7)

**User Story:** As a game designer, I want a complete decision brief rendered as structured JSON and readable markdown, so that I can present findings to stakeholders and use them in patch review meetings.

#### Acceptance Criteria

1. THE Report_Generator SHALL produce a response containing: executive_summary, who_is_affected, alignment, risks, solution_paths, validation_plan, and report_markdown
2. THE Report_Generator SHALL format who_is_affected as a list of objects with cohort, entity, and impact fields
3. THE Report_Generator SHALL format risks as a list of objects with id, severity, and evidence fields
4. THE Report_Generator SHALL format solution_paths as a list of objects with type, confidence, and rationale fields
5. THE Report_Generator SHALL generate report_markdown as a human-readable markdown document summarizing all sections
6. WHEN an LLM call fails or times out, THE Report_Generator SHALL fall back to a template-based report generated from the structured insights.json without LLM involvement

### Requirement 11: LLM Integration

**User Story:** As a pipeline developer, I want the AI layers to call an LLM with structured prompts, so that the system produces narrative-quality insights beyond deterministic rules.

#### Acceptance Criteria

1. THE Pipeline SHALL support OpenAI and Gemini as LLM providers, selectable via environment configuration
2. WHEN calling the LLM, THE Pipeline SHALL send a single structured prompt containing metrics, context_bundle, and deterministic insights as input
3. THE Pipeline SHALL constrain LLM output to the defined report JSON schema
4. IF the LLM returns a response that does not conform to the expected schema, THEN THE Pipeline SHALL fall back to the template-based report and log the schema violation
5. THE Pipeline SHALL include the LLM API key as an environment variable and never hardcode credentials in source

### Requirement 12: Frontend Upload and Report View

**User Story:** As a game designer, I want a web interface to upload my files and view the generated report, so that I can use the tool without API knowledge or command-line tools.

#### Acceptance Criteria

1. THE Frontend SHALL provide a drag-and-drop upload area accepting the required JSON files (player_online, player_offline, game_definition, rules, update_plan, community)
2. WHILE the pipeline is processing an upload, THE Frontend SHALL display a loading state indicating analysis is in progress
3. WHEN the pipeline returns a successful response, THE Frontend SHALL render the report with distinct sections for executive summary, affected cohorts, alignment, risks, solution paths, and validation plan
4. WHEN the pipeline returns an error response, THE Frontend SHALL display the error message in a user-readable format
5. THE Frontend SHALL render the report_markdown section as formatted markdown

### Requirement 13: Data Validation and Cross-File Integrity

**User Story:** As a pipeline developer, I want entity IDs validated across all uploaded files, so that mismatches are caught early before reaching AI analysis.

#### Acceptance Criteria

1. WHEN files are uploaded, THE Pipeline SHALL validate that entity_ids referenced in update_plan.json exist in game_definition.json
2. WHEN files are uploaded, THE Pipeline SHALL validate that entity_ids referenced in rules.json exist in game_definition.json
3. IF a cross-file entity_id mismatch is detected, THEN THE Pipeline SHALL return an error listing all unresolved references before proceeding to analysis
4. THE Pipeline SHALL validate all uploaded files against their respective JSON schemas defined in the schemas/ directory

### Requirement 14: Fixture and Demo Data

**User Story:** As a hackathon team member, I want pre-built demo fixtures with a golden case, so that I can test the pipeline end-to-end without real game data.

#### Acceptance Criteria

1. THE Repository SHALL include a `fixtures/demo_case/` directory containing valid instances of all seven input files (player_online, player_offline, game_definition, rules, update_plan, community, adapter)
2. THE Repository SHALL include fixture data representing a bracket_split_easy_low scenario where a tank character is strong in low brackets and average in high brackets
3. WHEN the demo_case fixtures are uploaded to the pipeline, THE Pipeline SHALL produce a complete report without errors
4. THE Repository SHALL include a second fixture set (`fixtures/demo_case_2/`) representing a perception_vs_data_divergence scenario where community says an entity is weak but metrics show it is average

### Requirement 15: Deployment Configuration

**User Story:** As the integration lead, I want deployment configurations for Railway (backend) and Vercel (frontend), so that the team can ship a live demo during the hackathon.

#### Acceptance Criteria

1. THE Repository SHALL include deployment configuration for the backend targeting Railway
2. THE Repository SHALL include deployment configuration for the frontend targeting Vercel
3. THE Pipeline SHALL operate without persistent storage (database or queue) for the MVP — all processing is stateless per request
4. THE Pipeline SHALL function without internet connectivity except for the LLM API call
