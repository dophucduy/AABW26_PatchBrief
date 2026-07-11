import { useEffect, useMemo, useRef, useState } from 'react';
import type { ReactElement } from 'react';
import { getHealth, hasApi, listAdapters, loadDemoFiles, runAnalysis, runDemoAnalysis } from './api';
import { demoReport, requiredFiles } from './data';
import { MappingDrawer } from './components/MappingDrawer';
import { AnalyzeWizard } from './components/AnalyzeWizard';
import { CohortDeltaBoard, ReportSnapshot } from './components/ReportVisuals';
import { AppShell, BackLink, Button, Eyebrow, Icon, RiskPill } from './components/Ui';
import { LandingPage } from './pages/LandingPage';
import type { AdapterSummary, AnalyzeFiles, AppRoute, MappingStage, PatchReport, Severity } from './types';

const routes: AppRoute[] = ['/', '/analyze', '/analyze/loading', '/report', '/mapping', '/mapping/review', '/mapping/confirm', '/error'];

function reportSectionId(): string | null {
  const raw = window.location.hash.replace(/^#/, '');
  return /^section-\d+$/.test(raw) ? raw : null;
}

function currentRoute(): AppRoute {
  const raw = window.location.hash.replace(/^#/, '') || '/';
  if (reportSectionId()) return '/report';
  const path = raw.startsWith('/') ? raw : `/${raw}`;
  return routes.includes(path as AppRoute) ? path as AppRoute : '/error';
}

function scrollToReportSection(id: string) {
  requestAnimationFrame(() => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  });
}

function navigate(path: AppRoute) {
  window.location.hash = path;
}

function isPatchReport(value: unknown): value is PatchReport {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as Partial<PatchReport>;
  return typeof candidate.report_id === 'string' && typeof candidate.executive_summary === 'string' && Array.isArray(candidate.risks);
}

function loadReport() {
  try {
    const parsed: unknown = JSON.parse(sessionStorage.getItem('patchBriefReport') || 'null');
    return isPatchReport(parsed) ? parsed : demoReport;
  } catch {
    return demoReport;
  }
}

interface AnalyzeWorkspaceProps {
  onNavigate: (path: AppRoute) => void;
  initialMappingStage?: MappingStage;
}

function AnalyzeWorkspace({ onNavigate, initialMappingStage }: AnalyzeWorkspaceProps) {
  const [files, setFiles] = useState<AnalyzeFiles>({});
  const [gameName, setGameName] = useState('');
  const [adapters, setAdapters] = useState<AdapterSummary[]>([{ adapter_id: 'demo_moba', created_at: '2026-07-11T08:00:00Z' }]);
  const [adapterId, setAdapterId] = useState('demo_moba');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [loadingDemo, setLoadingDemo] = useState(false);
  const [wizardJumpToken, setWizardJumpToken] = useState(0);
  const [mappingOpen, setMappingOpen] = useState(Boolean(initialMappingStage));
  const mappingTriggerRef = useRef<HTMLButtonElement>(null);
  const filesReady = requiredFiles.every((file) => Boolean(files[file.key]));
  const gameNameReady = !hasApi || gameName.trim().length > 0;
  const requiredReady = filesReady && gameNameReady;

  useEffect(() => {
    if (!hasApi) return;
    listAdapters().then((response) => {
      if (!response.adapters?.length) return;
      setAdapters(response.adapters);
      setAdapterId(response.adapters[0].adapter_id);
    }).catch(() => undefined);
  }, []);

  const selectFile = (key: string, file: File) => {
    if (!file.name.toLowerCase().endsWith('.json')) {
      setError(`${file.name} is not a JSON file. Choose a .json file.`);
      return;
    }
    setFiles((current) => ({ ...current, [key]: file }));
    setError('');
  };

  const jsonFile = (name: string, content: string) =>
    new File([content], name, { type: 'application/json' });

  const loadDemo = async () => {
    if (!hasApi) {
      setError('Backend is offline. Start the API on port 5278 first.');
      return;
    }
    setLoadingDemo(true);
    setError('');
    try {
      const fixtures = await loadDemoFiles();
      setFiles({
        telemetry: jsonFile('telemetry.json', fixtures.telemetry),
        game_definition: jsonFile('game_definition.json', fixtures.game_definition),
        rules: jsonFile('rules.json', fixtures.rules),
        update_plan: jsonFile('update_plan.json', fixtures.update_plan),
      });
      setAdapterId(fixtures.adapter_id || 'demo_moba');
      setGameName('Dota 2');
      setWizardJumpToken((current) => current + 1);
    } catch (caught: unknown) {
      const message = caught instanceof Error ? caught.message : 'Could not load demo fixtures.';
      setError(message);
    } finally {
      setLoadingDemo(false);
    }
  };

  const startDemoAnalysis = () => {
    if (!hasApi || submitting) return;
    setSubmitting(true);
    sessionStorage.removeItem('patchBriefReport');
    sessionStorage.removeItem('patchBriefError');
    sessionStorage.setItem('patchBriefAnalysisMode', 'demo');
    sessionStorage.setItem('patchBriefStartedAt', String(Date.now()));
    navigate('/analyze/loading');
    runDemoAnalysis().then((report) => {
      sessionStorage.setItem('patchBriefReport', JSON.stringify(report));
    }).catch((caught: unknown) => {
      const message = caught instanceof Error ? caught.message : 'Demo analysis failed.';
      sessionStorage.setItem('patchBriefError', message);
      sessionStorage.removeItem('patchBriefReport');
    }).finally(() => setSubmitting(false));
  };

  const saveNewAdapter = (adapter: AdapterSummary) => {
    setAdapters((current) => current.some((item) => item.adapter_id === adapter.adapter_id) ? current : [...current, adapter]);
    setAdapterId(adapter.adapter_id);
  };

  const startAnalysis = () => {
    if (!requiredReady || submitting) return;
    setSubmitting(true);
    sessionStorage.removeItem('patchBriefReport');
    sessionStorage.removeItem('patchBriefError');
    sessionStorage.setItem('patchBriefAnalysisMode', 'full');
    sessionStorage.setItem('patchBriefStartedAt', String(Date.now()));
    const request = hasApi ? runAnalysis(files, gameName, adapterId) : Promise.resolve(demoReport);
    navigate('/analyze/loading');
    request.then((report) => {
      sessionStorage.setItem('patchBriefReport', JSON.stringify(report));
    }).catch((caught: unknown) => {
      const message = caught instanceof Error ? caught.message : 'Analysis failed.';
      sessionStorage.setItem('patchBriefError', message);
      sessionStorage.removeItem('patchBriefReport');
    }).finally(() => setSubmitting(false));
  };

  return <div className="workspace page-frame workspace--wizard">
    <BackLink label="Back to home" onClick={() => onNavigate('/')} />
    <header className="workspace-heading workspace-heading--compact">
      <div>
        <Eyebrow>Analysis workspace</Eyebrow>
        <h1>Build the brief<br /><em>from the evidence.</em></h1>
      </div>
      <p>One step at a time — mapping, files, community signal, then run analysis.</p>
    </header>

    <AnalyzeWizard
      files={files}
      gameName={gameName}
      adapters={adapters}
      adapterId={adapterId}
      error={error}
      submitting={submitting}
      loadingDemo={loadingDemo}
      mappingTriggerRef={mappingTriggerRef}
      jumpToReviewToken={wizardJumpToken}
      onAdapterChange={setAdapterId}
      onGameNameChange={setGameName}
      onSelectFile={selectFile}
      onOpenMapping={() => setMappingOpen(true)}
      onLoadDemo={() => void loadDemo()}
      onRunDemo={startDemoAnalysis}
      onRunAnalysis={startAnalysis}
    />

    <MappingDrawer isOpen={mappingOpen} initialStage={initialMappingStage} triggerRef={mappingTriggerRef} onClose={() => setMappingOpen(false)} onAdapterSaved={saveNewAdapter} />
  </div>;
}

function LoadingPage({ onNavigate }: { onNavigate: (path: AppRoute) => void }) {
  const isDemo = sessionStorage.getItem('patchBriefAnalysisMode') === 'demo';
  const steps = isDemo
    ? ['Adapt telemetry', 'Metrics and cohorts', 'Context merge', 'Impact and risk', 'Generating brief']
    : ['Ingest and normalize', 'Scrape Steam reviews', 'Metrics and cohorts', 'Context merge', 'Impact and risk', 'Generating brief'];
  const [activeStep, setActiveStep] = useState(0);
  useEffect(() => {
    const stepTimer = window.setInterval(
      () => setActiveStep((current) => Math.min(current + 1, steps.length - 1)),
      isDemo ? 2200 : 2800,
    );
    let waitTimer: number | undefined;
    const waitForReport = () => {
      const hasReport = Boolean(sessionStorage.getItem('patchBriefReport'));
      const hasError = Boolean(sessionStorage.getItem('patchBriefError'));
      const onLastStep = activeStep >= steps.length - 1;
      if (hasError) {
        onNavigate('/error');
        return;
      }
      if (hasReport && onLastStep) {
        onNavigate('/report');
        return;
      }
      const startedAt = Number(sessionStorage.getItem('patchBriefStartedAt') || Date.now());
      if (Date.now() - startedAt > 120000) {
        sessionStorage.setItem('patchBriefError', 'Analysis timed out. Please try again.');
        onNavigate('/error');
        return;
      }
      waitTimer = window.setTimeout(waitForReport, 300);
    };
    const reportTimer = window.setTimeout(waitForReport, 1200);
    return () => {
      window.clearInterval(stepTimer);
      window.clearTimeout(reportTimer);
      if (waitTimer) window.clearTimeout(waitTimer);
    };
  }, [activeStep, isDemo, onNavigate, steps.length]);
  return <div className="loading-page page-frame"><section className="loading-card"><div className="loading-radar"><Icon name="scan" size={30} /></div><Eyebrow>Pipeline active</Eyebrow><h1>Reading the signal<br /><em>behind the patch.</em></h1><p>Each layer is framing evidence for a human design decision.</p><div className="loading-progress"><span style={{ width: `${((activeStep + 1) / steps.length) * 100}%` }}></span></div><ol>{steps.map((step, index) => <li key={step} className={index < activeStep ? 'done' : index === activeStep ? 'current' : ''}><span>{index < activeStep ? <Icon name="check" size={14} /> : String(index + 1).padStart(2, '0')}</span><strong>{step}</strong>{index === activeStep && <small>working</small>}</li>)}</ol></section></div>;
}

function reportDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Generated report';
  return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date).toUpperCase();
}

function highestRisk(report: PatchReport): Severity {
  if (report.overview?.overall_risk) return report.overview.overall_risk;
  const rank: Record<Severity, number> = { low: 1, medium: 2, high: 3 };
  return report.risks.reduce<Severity>((current, risk) => rank[risk.severity] > rank[current] ? risk.severity : current, 'low');
}

const REPORT_SECTIONS = [
  'Who will be affected',
  'What part of the game will be affected',
  'Community compare',
  'The risk dev may face',
  'Suggest solution',
  'Summary',
] as const;

function ReportPage({ onNavigate }: { onNavigate: (path: AppRoute) => void }) {
  const report = useMemo(loadReport, []);
  const [copied, setCopied] = useState(false);
  const primaryEntity = report.who_is_affected.find((entity) => entity.impact === 'high') || report.who_is_affected[0];
  const overallRisk = highestRisk(report);
  const isDemo = report.report_mode === 'demo';
  const reportBadge = isDemo
    ? 'Demo preview'
    : report.llm_used
      ? 'LLM assisted'
      : 'Template fallback';
  const copyDraft = async () => {
    try {
      await navigator.clipboard.writeText(report.report_markdown || report.draft_player_comms);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1400);
    } catch {
      setCopied(false);
    }
  };
  return <div className="report-page">
    <section className="report-hero"><div className="report-hero-frame page-frame"><BackLink label="Back to analyze" onClick={() => onNavigate('/analyze')} /><div className="report-hero-grid"><div className="report-summary"><Eyebrow>Impact brief / {report.report_id}</Eyebrow><h1>{primaryEntity?.entity_name || 'Patch'}<br /><em>decision surface.</em></h1><p>Pre-ship balance brief — evidence, alignment, risks, and suggested paths for your team to decide.</p><div className="report-meta"><span>{reportDate(report.generated_at)}</span><span></span><b><i></i>{reportBadge}</b></div></div><div className="report-risk"><span>Overall exposure</span><strong>{overallRisk}</strong><RiskPill level={overallRisk} /><div className={`risk-bars ${overallRisk}`} aria-label={`${overallRisk} overall risk`}><i></i><i></i><i></i><i></i></div></div></div></div></section>
    <ReportSnapshot overview={report.overview} entities={report.who_is_affected} />
    <div className="report-layout page-frame"><article className="report-content">
      <section id="section-0" className="report-section">
        <SectionHeading index="01" title="Who will be affected" note="players and cohorts" />
        <CohortDeltaBoard entities={report.who_is_affected} />
      </section>

      <section id="section-1" className="report-section">
        <SectionHeading index="02" title="What part of the game will be affected" note="from update plan" />
        <div className="change-board">{report.proposed_changes.map((change) => <article key={`${change.target}-${change.field}`}><header><div><strong>{change.entity_name}</strong><small>{change.role} / {change.target}</small></div><span>{change.field}</span></header><div className="change-values"><b>{change.from}</b><Icon name="arrow-right" size={17} /><b>{change.to}</b><em>{change.delta}</em></div></article>)}</div>
      </section>

      <section id="section-2" className="report-section">
        <SectionHeading index="03" title="Community compare" note="player voice vs live data" />
        <div className="alignment-grid">
          <article className="alignment-card divergent">
            <span>Data versus community</span>
            <div><strong>{report.alignment.data_vs_community}</strong><b>{report.overview?.community_mentions ?? '—'}<small>{report.overview?.community_mentions === undefined ? '' : ' mentions'}</small></b></div>
            <p>How community sentiment compares to telemetry in this evidence set.</p>
          </article>
          <article className="alignment-card aligned">
            <span>Playtest versus live</span>
            <div><strong>{report.alignment.playtest_vs_live}</strong><b>Source read</b></div>
            <p>Whether offline playtest signal matches live population behavior.</p>
          </article>
        </div>
        <div className="pattern-list">{report.alignment.patterns.map((pattern) => <div key={pattern.id}><Icon name="scan" size={17} /><div><strong>{pattern.title}</strong><span>{pattern.description}</span></div><b className={pattern.confidence}>{pattern.confidence}</b></div>)}</div>
      </section>

      <section id="section-3" className="report-section">
        <SectionHeading index="04" title="The risk dev may face" note="before you lock the patch" />
        <div className="risk-grid">{report.risks.map((risk) => <article key={risk.id} className={`risk-card ${risk.severity}`}><div><RiskPill level={risk.severity} /><span>{risk.evidence.length} signals</span></div><h3>{risk.title}</h3><ul>{risk.evidence.map((evidence) => <li key={evidence}>{evidence}</li>)}</ul></article>)}</div>
      </section>

      <section id="section-4" className="report-section">
        <SectionHeading index="05" title="Suggest solution" note="paths, not prescriptions" />
        <div className="solution-list">{report.solution_paths.map((path, index) => <article key={path.type}><span>0{index + 1}</span><div><strong>{path.label}</strong><p>{path.rationale}</p></div><b className={path.confidence}>{path.confidence}</b></article>)}</div>
        {report.validation_plan.length > 0 && <>
          <header className="section-heading section-heading--sub"><div><span>05b</span><h2>Validation steps</h2></div><small>before shipping</small></header>
          <ol className="validation-list">{report.validation_plan.map((step, index) => <li key={step}><span>0{index + 1}</span><strong>{step}</strong><Icon name="check" size={16} /></li>)}</ol>
        </>}
      </section>

      <section id="section-5" className="report-section report-section--summary">
        <SectionHeading index="06" title="Summary" note="executive readout" />
        <div className="report-summary-block">
          <p>{report.executive_summary}</p>
        </div>
        <section className="comms-card">
          <div><Eyebrow>Draft player comms</Eyebrow><p>{report.draft_player_comms}</p></div>
          <Button variant="outline" onClick={copyDraft} icon={copied ? 'check' : 'copy'}>{copied ? 'Copied' : 'Copy draft'}</Button>
        </section>
      </section>
    </article><aside className="report-index"><div><span>Brief index</span>{REPORT_SECTIONS.map((label, index) => <button key={label} type="button" className="report-index-link" onClick={() => {
      const id = `section-${index}`;
      window.location.hash = id;
      scrollToReportSection(id);
    }}><b>0{index + 1}</b>{label}</button>)}<Button variant="secondary" onClick={() => onNavigate('/analyze')} icon="plus">New analysis</Button></div></aside></div>
  </div>;
}

function SectionHeading({ index, title, note }: { index: string; title: string; note?: string }) {
  return <header className="section-heading"><div><span>{index}</span><h2>{title}</h2></div>{note && <small>{note}</small>}</header>;
}

function ErrorPage({ onNavigate }: { onNavigate: (path: AppRoute) => void }) {
  const message = sessionStorage.getItem('patchBriefError') || 'The signal could not be completed.';
  return <div className="error-page page-frame"><section><Icon name="warning" size={32} /><Eyebrow>Analysis stopped</Eyebrow><h1>We lost the<br /><em>signal path.</em></h1><p>{message}</p><Button onClick={() => onNavigate('/analyze')} icon="arrow-left">Back to analysis</Button></section></div>;
}

export default function App() {
  const [route, setRoute] = useState<AppRoute>(currentRoute);
  const [apiOnline, setApiOnline] = useState(false);
  useEffect(() => {
    const handleRoute = () => {
      setRoute(currentRoute());
      const sectionId = reportSectionId();
      if (sectionId) scrollToReportSection(sectionId);
    };
    window.addEventListener('hashchange', handleRoute);
    handleRoute();
    return () => window.removeEventListener('hashchange', handleRoute);
  }, []);
  useEffect(() => {
    if (reportSectionId()) return;
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' });
  }, [route]);
  useEffect(() => {
    if (!hasApi) return;
    getHealth().then(() => setApiOnline(true)).catch(() => setApiOnline(false));
  }, []);

  const routeTo = (path: string) => navigate(path as AppRoute);
  let page: ReactElement;
  if (route === '/') page = <LandingPage onNavigate={routeTo} />;
  else if (route === '/analyze') page = <AnalyzeWorkspace onNavigate={routeTo} />;
  else if (route === '/mapping') page = <AnalyzeWorkspace key="mapping-upload" onNavigate={routeTo} initialMappingStage="upload" />;
  else if (route === '/mapping/review') page = <AnalyzeWorkspace key="mapping-review" onNavigate={routeTo} initialMappingStage="review" />;
  else if (route === '/mapping/confirm') page = <AnalyzeWorkspace key="mapping-confirm" onNavigate={routeTo} initialMappingStage="confirm" />;
  else if (route === '/analyze/loading') page = <LoadingPage onNavigate={routeTo} />;
  else if (route === '/report') page = <ReportPage onNavigate={routeTo} />;
  else page = <ErrorPage onNavigate={routeTo} />;
  return <AppShell currentPath={route} apiOnline={apiOnline} onNavigate={routeTo}>{page}</AppShell>;
}
