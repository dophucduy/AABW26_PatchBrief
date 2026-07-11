/**
 * Smoke test: calls every endpoint in frontend/src/api.ts against the backend.
 * Usage: node scripts/api-smoke-test.mjs [baseUrl]
 */
const baseUrl = (process.argv[2] || process.env.VITE_API_URL || 'http://localhost:5278').replace(/\/$/, '');

async function request(path, options = {}) {
  const response = await fetch(`${baseUrl}${path}`, options);
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(payload?.error?.message || `${path} failed (${response.status})`);
  }
  return payload;
}

function jsonFile(name, content) {
  return new File([content], name, { type: 'application/json' });
}

async function loadFixture(name) {
  const path = new URL(`../../fixtures/demo_case/${name}`, import.meta.url);
  const { readFile } = await import('node:fs/promises');
  return readFile(path, 'utf8');
}

async function main() {
  const fixtures = await FixtureLoader.load();
  console.log(`Testing API at ${baseUrl}`);

  const health = await request('/api/analyze/health');
  console.log('✓ GET /api/analyze/health', health.status);

  const adapters = await request('/api/mapping');
  console.log('✓ GET /api/mapping', `${adapters.adapters?.length || 0} adapters`);

  const suggestForm = new FormData();
  suggestForm.append('sampleFile', jsonFile('telemetry_live.json', fixtures.telemetry));
  suggestForm.append('genre', 'MOBA');
  const suggestion = await request('/api/mapping/suggest', { method: 'POST', body: suggestForm });
  console.log('✓ POST /api/mapping/suggest', `${suggestion.field_map?.length || 0} field maps`);

  const previewForm = new FormData();
  previewForm.append('sampleFile', jsonFile('telemetry_live.json', fixtures.telemetry));
  previewForm.append('fieldMap', JSON.stringify([{ source: 'hero', target: 'entity_id', confidence: 0.9, kind: 'Field' }]));
  previewForm.append('eventMap', JSON.stringify([]));
  const preview = await request('/api/mapping/preview', { method: 'POST', body: previewForm });
  console.log('✓ POST /api/mapping/preview', `${preview.events_parsed} parsed`);

  await request('/api/mapping/confirm', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      adapter_id: 'frontend_smoke_adapter',
      canonical_version: '1.0',
      field_map: { hero: 'entity_id' },
      event_map: {},
      custom_fields: {},
      confirmed_by_user: true,
    }),
  });
  console.log('✓ POST /api/mapping/confirm');

  const demo = await request('/api/analyze/demo');
  console.log('✓ GET /api/analyze/demo', demo.report_id);

  if (process.env.APIFY_TOKEN || process.env.Apify__ApiToken) {
    const analyzeForm = new FormData();
    analyzeForm.append('telemetry', jsonFile('telemetry.json', fixtures.telemetry));
    analyzeForm.append('gameDefinition', jsonFile('game_definition.json', fixtures.game_definition));
    analyzeForm.append('rules', jsonFile('rules.json', fixtures.rules));
    analyzeForm.append('updatePlan', jsonFile('update_plan.json', fixtures.update_plan));
    analyzeForm.append('gameName', 'Dota 2');
    analyzeForm.append('adapterId', 'demo_moba');
    const report = await request('/api/analyze', { method: 'POST', body: analyzeForm });
    console.log('✓ POST /api/analyze', report.report_id);
  } else {
    console.log('↷ POST /api/analyze skipped (set APIFY_TOKEN to test Steam scrape path)');
  }

  console.log('\nAll frontend API endpoints passed.');
}

const FixtureLoader = {
  async load() {
    const telemetry = await loadFixture('telemetry_live.json');
    const game_definition = await loadFixture('game_definition.json');
    const context_bundle = JSON.parse(await loadFixture('context_bundle.json'));
    return {
      telemetry,
      game_definition,
      rules: JSON.stringify(context_bundle.rules),
      update_plan: JSON.stringify(context_bundle.update_plan),
    };
  },
};

main().catch((error) => {
  console.error('API smoke test failed:', error.message);
  process.exit(1);
});
