export const demoReport = {
  report_id: 'rpt_20260711_001',
  generated_at: '2026-07-11T10:00:00Z',
  llm_used: true,
  executive_summary:
    'The proposed Ironclad nerf affects 38% of low-bracket sessions. Community perception diverges from the data — likely kit feel, not raw stats. Backlash risk is high if shipped without communication.',
  who_is_affected: [
    { entity_id: 'char_A', entity_name: 'Ironclad', role: 'Tank', cohort: 'Low bracket', impact: 'high', metric_refs: ['Pick rate 22%', 'Win rate 58%'] },
    { entity_id: 'char_A', entity_name: 'Ironclad', role: 'Tank', cohort: 'High bracket', impact: 'low', metric_refs: ['Pick rate 18%', 'Win rate 49%'] },
    { entity_id: 'char_B', entity_name: 'Wisp', role: 'Support', cohort: 'All brackets', impact: 'medium', metric_refs: ['Pick rate 11%', 'Win rate 52%'] },
  ],
  proposed_changes: [
    { target: 'char_A', entity_name: 'Ironclad', field: 'Base damage', from: 45, to: 40, delta: '-11%', role: 'Tank' },
    { target: 'char_A', entity_name: 'Ironclad', field: 'Guard duration', from: '2.5s', to: '2.0s', delta: '-20%', role: 'Tank' },
    { target: 'char_B', entity_name: 'Wisp', field: 'Ultimate charge', from: 100, to: 90, delta: '-10%', role: 'Support' },
  ],
  alignment: {
    data_vs_community: 'divergent',
    playtest_vs_live: 'aligned',
    patterns: [
      { id: 'bracket_split', title: 'Bracket split', description: 'Strong in low bracket, mediocre in high', confidence: 'high' },
      { id: 'perception', title: 'Perception vs data', description: 'Community reports weak; win rate near meta average', confidence: 'medium' },
    ],
  },
  risks: [
    { id: 'stakeholder_conflict', severity: 'high', title: 'Casual players favor buff; ranked meta stable', evidence: ['Win rate high bracket: 49% near average', "Community theme 'feels_weak': 340 mentions"] },
    { id: 'second_order_meta', severity: 'medium', title: 'Tank nerf may shift team comp meta', evidence: ['Ironclad pick rate in tank role: 18%', 'Wisp compensates in 14% of tracked comps'] },
    { id: 'identity_lever', severity: 'low', title: 'Guard fantasy could feel less reliable', evidence: ['Guard duration is a signature interaction', 'Playtest feedback mentions timing, not power'] },
  ],
  solution_paths: [
    { type: 'targeted_by_bracket', label: 'Target by bracket', confidence: 'medium', rationale: 'Low bracket overperforms; high bracket is healthy. Avoid a global buff.', designer_decides: true },
    { type: 'comms_only', label: 'Communication only', confidence: 'medium', rationale: 'Data is near average; address perception before numeric change.' },
    { type: 'tune_numbers', label: 'Tune the numbers', confidence: 'low', rationale: 'Only if playtest confirms the low-bracket spike after a targeted test.' },
  ],
  validation_plan: [
    'Playtest Ironclad in low vs high bracket after the micro-adjustment',
    'Monitor pick-rate split for one week post-patch',
    'Survey: fun to play vs feels weak',
  ],
  draft_player_comms: "We've heard feedback on Ironclad. Current data shows performance near average in skilled play; we're reviewing readability and game feel before making balance changes.",
};

export const mappingRows = [
  { source: 'characterId', sample: 'char_A', target: 'entity_id', confidence: 0.92, kind: 'Field' },
  { source: 'gameTime', sample: '45.1', target: 't', confidence: 0.78, kind: 'Field' },
  { source: 'playerRank', sample: 'bronze_2', target: 'bracket', confidence: 0.68, kind: 'Field' },
  { source: 'Death', sample: 'Death', target: 'death', confidence: 0.91, kind: 'Event' },
  { source: 'MatchEnd', sample: 'MatchEnd', target: 'match_end', confidence: 0.88, kind: 'Event' },
];

export const canonicalOptions = ['entity_id', 't', 'session_id', 'bracket', 'area_id', 'cause_id', 'source', 'win_rate', 'pick_rate', 'finish_time', 'death_rate', 'death', 'match_end', 'ability_used', 'entity_pick', 'area_enter', 'session_start'];

export const requiredFiles = [
  { key: 'player_online', label: 'player_online.json', description: 'Live telemetry' },
  { key: 'player_offline', label: 'player_offline.json', description: 'Playtest sessions' },
  { key: 'game_definition', label: 'game_definition.json', description: 'Roster, stats, roles' },
  { key: 'rules', label: 'rules.json', description: 'Locked and open levers' },
  { key: 'update_plan', label: 'update_plan.json', description: 'Planned changes' },
  { key: 'community', label: 'community.json', description: 'Sentiment and posts', optional: true },
];

