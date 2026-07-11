export type ImpactLevel = 'high' | 'medium' | 'low';
export type Severity = ImpactLevel;
export type Confidence = 'high' | 'medium' | 'low';
export type AlignmentStatus = 'aligned' | 'divergent' | 'mixed';
export type MappingKind = 'Field' | 'Event';
export type MappingStage = 'upload' | 'review' | 'confirm';
export type AppRoute = '/' | '/analyze' | '/analyze/loading' | '/report' | '/mapping' | '/mapping/review' | '/mapping/confirm' | '/error';

export interface MappingRow {
  source: string;
  sample: string;
  target: string;
  confidence: number;
  kind: MappingKind;
}

export interface RequiredFile {
  key: string;
  label: string;
  description: string;
  optional?: boolean;
}

export type AnalyzeFiles = Record<string, File | undefined>;

export interface AdapterSummary {
  adapter_id: string;
  created_at?: string;
}

export interface AffectedEntity {
  entity_id: string;
  entity_name: string;
  role: string;
  cohort: string;
  impact: ImpactLevel;
  metric_refs: string[];
}

export interface ProposedChange {
  target: string;
  entity_name: string;
  field: string;
  from: string | number;
  to: string | number;
  delta: string;
  role: string;
}

export interface AlignmentPattern {
  id: string;
  title: string;
  description: string;
  confidence: Confidence;
}

export interface Risk {
  id: string;
  severity: Severity;
  title: string;
  evidence: string[];
}

export interface SolutionPath {
  type: string;
  label: string;
  confidence: Confidence;
  rationale: string;
  designer_decides?: boolean;
}

export interface PatchReport {
  report_id: string;
  generated_at: string;
  llm_used: boolean;
  executive_summary: string;
  who_is_affected: AffectedEntity[];
  proposed_changes: ProposedChange[];
  alignment: {
    data_vs_community: AlignmentStatus;
    playtest_vs_live: AlignmentStatus;
    patterns: AlignmentPattern[];
  };
  risks: Risk[];
  solution_paths: SolutionPath[];
  validation_plan: string[];
  draft_player_comms: string;
  report_markdown?: string;
}

export interface MappingPreviewData {
  events_parsed: number;
  events_skipped: number;
  warnings: string[];
}
