export type MappingSuggestion = {
  field_map?: Array<{ source: string; target: string; confidence?: number }>;
  event_map?: Array<{ source: string; target: string; confidence?: number }>;
  [key: string]: unknown;
};

export type MappingPreview = {
  events_parsed: number;
  events_skipped: number;
  warnings: string[];
  [key: string]: unknown;
};

export type AdapterPayload = {
  adapter_id: string;
  canonical_version: string;
  field_map: Record<string, string>;
  event_map: Record<string, string>;
  custom_fields: Record<string, unknown>;
  confirmed_by_user: boolean;
};

export type AnalyzeFiles = Record<string, File | undefined>;
export type ReportResponse = Record<string, unknown>;

const apiBase = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '');

export const hasApi = Boolean(apiBase);

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, options);
  const payload = await response.json().catch(() => ({})) as { error?: { message?: string } };
  if (!response.ok) throw new Error(payload?.error?.message || `Request failed (${response.status})`);
  return payload as T;
}

export const getHealth = () => request('/api/analyze/health');

export const suggestMapping = (file: File, genre?: string) => {
  const form = new FormData();
  form.append('sampleFile', file);
  if (genre) form.append('genre', genre);
  return request<MappingSuggestion>('/api/mapping/suggest', { method: 'POST', body: form });
};

export const previewMapping = (file: File, fieldMap: unknown[], eventMap: unknown[]) => {
  const form = new FormData();
  form.append('sampleFile', file);
  form.append('fieldMap', JSON.stringify(fieldMap));
  form.append('eventMap', JSON.stringify(eventMap));
  return request<MappingPreview>('/api/mapping/preview', { method: 'POST', body: form });
};

export const confirmMapping = (payload: AdapterPayload) => request<Record<string, unknown>>('/api/mapping/confirm', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(payload),
});

export const listAdapters = () => request<{ adapters?: Array<{ adapter_id: string; created_at: string }> }>('/api/mapping');

export const runAnalysis = (files: AnalyzeFiles, adapterId?: string) => {
  const form = new FormData();
  const fieldNames: Record<string, string> = {
    player_online: 'playerOnline',
    player_offline: 'playerOffline',
    game_definition: 'gameDefinition',
    update_plan: 'updatePlan',
  };
  Object.entries(files).forEach(([key, file]) => file && form.append(fieldNames[key] || key, file));
  if (adapterId) form.append('adapterId', adapterId);
  return request<ReportResponse>('/api/analyze', { method: 'POST', body: form });
};
