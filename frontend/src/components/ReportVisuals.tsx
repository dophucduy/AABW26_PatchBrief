import type { AffectedEntity, CohortMetrics, ReportOverview } from '../types';
import { RiskPill } from './Ui';

type MetricKey = keyof CohortMetrics;

interface CohortGroup {
  entityId: string;
  entityName: string;
  role: string;
  impact: AffectedEntity['impact'];
  cohorts: AffectedEntity[];
}

const metricLabels: Record<MetricKey, string> = {
  win_rate: 'Win rate',
  pick_rate: 'Pick rate',
};

function groupAffectedEntities(entities: AffectedEntity[]): CohortGroup[] {
  const groups = new Map<string, AffectedEntity[]>();
  entities.forEach((entity) => {
    const group = groups.get(entity.entity_id) || [];
    group.push(entity);
    groups.set(entity.entity_id, group);
  });

  return [...groups.entries()].map(([entityId, cohorts]) => {
    const lead = cohorts.find((entity) => entity.impact === 'high') || cohorts[0];
    return {
      entityId,
      entityName: lead.entity_name,
      role: lead.role,
      impact: lead.impact,
      cohorts,
    };
  });
}

function valuesForMetric(cohorts: AffectedEntity[], key: MetricKey) {
  return cohorts.flatMap((cohort) => {
    const value = cohort.metrics?.[key];
    return typeof value === 'number' ? [{ cohort, value }] : [];
  });
}

function metricSpread(cohorts: AffectedEntity[], key: MetricKey) {
  const values = valuesForMetric(cohorts, key).map((item) => item.value);
  return values.length > 1 ? Math.max(...values) - Math.min(...values) : undefined;
}

function formatValue(value: number | undefined) {
  return value === undefined ? '—' : `${value}%`;
}

function formatSpread(value: number | undefined) {
  return value === undefined ? 'Single cohort' : `${value} pp spread`;
}

function MetricComparison({ cohorts, metric }: { cohorts: AffectedEntity[]; metric: MetricKey }) {
  const values = valuesForMetric(cohorts, metric);
  if (!values.length) return null;

  const spread = metricSpread(cohorts, metric);
  const description = values.map((item) => `${item.cohort.cohort} ${formatValue(item.value)}`).join(', ');

  return <div className={`metric-comparison ${metric}`}>
    <div className="metric-comparison-heading"><span>{metricLabels[metric]}</span><b>{formatSpread(spread)}</b></div>
    <div className="metric-comparison-bars" role="img" aria-label={`${metricLabels[metric]}: ${description}`}>
      {values.map(({ cohort, value }) => <div className="metric-bar-row" key={`${cohort.entity_id}-${cohort.cohort}-${metric}`}>
        <span>{cohort.cohort}</span>
        <div className="metric-bar-track"><i style={{ width: `${Math.min(Math.max(value, 3), 100)}%` }}></i></div>
        <strong>{formatValue(value)}</strong>
      </div>)}
    </div>
  </div>;
}

export function ReportSnapshot({ overview, entities }: { overview?: ReportOverview; entities: AffectedEntity[] }) {
  const groups = groupAffectedEntities(entities);
  const primary = groups.find((group) => group.impact === 'high') || groups[0];
  const winRateSpread = primary ? metricSpread(primary.cohorts, 'win_rate') : undefined;

  return <section className="report-snapshot page-frame" aria-label="Evidence snapshot">
    <article><span>Sessions in frame</span><strong>{overview?.affected_sessions_percent ?? '—'}<small>{overview ? '%' : ''}</small></strong><p>affected in the primary cohort</p></article>
    <article><span>Win-rate spread</span><strong>{winRateSpread ?? '—'}<small>{winRateSpread === undefined ? '' : ' pp'}</small></strong><p>{primary ? `${primary.entityName}: cohort-to-cohort gap` : 'No cohort comparison available'}</p></article>
    <article><span>Player voice</span><strong>{overview?.community_mentions ?? '—'}</strong><p>{overview?.community_mentions === undefined ? 'No community volume supplied' : 'negative mentions in the evidence'}</p></article>
  </section>;
}

export function CohortDeltaBoard({ entities }: { entities: AffectedEntity[] }) {
  return <div className="cohort-board">
    {groupAffectedEntities(entities).map((group) => <article className="cohort-card" key={group.entityId}>
      <header><div><span>{group.entityId}</span><h3>{group.entityName}</h3><p>{group.role}</p></div><RiskPill level={group.impact} /></header>
      <MetricComparison cohorts={group.cohorts} metric="win_rate" />
      <MetricComparison cohorts={group.cohorts} metric="pick_rate" />
    </article>)}
  </div>;
}
