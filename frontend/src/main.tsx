import React, { useEffect, useMemo, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { canonicalOptions, demoReport, mappingRows, requiredFiles } from './data';
import { confirmMapping, getHealth, hasApi, listAdapters, previewMapping, runAnalysis, suggestMapping } from './api';
import './styles.css';

type IconName = 'arrow' | 'check' | 'chevron' | 'upload' | 'spark' | 'play' | 'pulse' | 'copy' | 'external';

const route = () => window.location.hash.replace(/^#/, '') || '/';
const go = (path) => { window.location.hash = path; };
let mappingSampleFile: File | null = null;

function Icon({ name, size = 18 }: { name: IconName; size?: number }) {
  const paths = {
    arrow: <><path d="M4 12h15"/><path d="m13 6 6 6-6 6"/></>,
    check: <path d="m5 12 4 4L19 6"/>,
    chevron: <path d="m7 10 5 5 5-5"/>,
    upload: <><path d="M12 16V4"/><path d="m7 9 5-5 5 5"/><path d="M5 20h14"/></>,
    spark: <><path d="m12 3 1.7 5.3L19 10l-5.3 1.7L12 17l-1.7-5.3L5 10l5.3-1.7L12 3Z"/><path d="m19 16 .7 2.3L22 19l-2.3.7L19 22l-.7-2.3L16 19l2.3-.7L19 16Z"/></>,
    play: <path d="m8 5 11 7-11 7V5Z"/>,
    pulse: <path d="M3 12h3l2-7 4 14 2-7h7"/>,
    copy: <><rect x="9" y="9" width="11" height="11" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></>,
    external: <><path d="M14 3h7v7"/><path d="M10 14 21 3"/><path d="M21 14v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/></>,
  };
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}

function AppShell({ children, health }) {
  const current = route();
  return <div className="app-shell">
    <header className="topbar">
      <button className="brand" onClick={() => go('/')} aria-label="Go home">
        <span className="brand-mark"><span></span><span></span><span></span></span>
        <span>PATCH<span className="brand-slash">/</span>BRIEF</span>
      </button>
      <nav className="main-nav" aria-label="Main navigation">
        <button className={current.startsWith('/analyze') ? 'active' : ''} onClick={() => go('/analyze')}>Analyze</button>
        <button className={current.startsWith('/mapping') ? 'active' : ''} onClick={() => go('/mapping')}>Mapping setup</button>
        <button className={current === '/report' ? 'active' : ''} onClick={() => go('/report')}>Latest report</button>
      </nav>
      <div className="topbar-meta"><span className={`health-dot ${health === 'online' ? 'online' : ''}`}></span><span>{health === 'online' ? 'API connected' : 'Demo workspace'}</span></div>
    </header>
    <main>{children}</main>
    <footer className="site-footer"><span>PATCH / BRIEF · Balance intelligence for live games</span><span>v0.1 · {hasApi ? 'Connected to API' : 'Local demo mode'}</span></footer>
  </div>;
}

function Eyebrow({ children }: { children: React.ReactNode }) { return <div className="eyebrow"><span className="eyebrow-line"></span>{children}</div>; }
function Button({ children, onClick, variant = 'primary', icon, type = 'button', disabled = false }: { children: React.ReactNode; onClick?: () => void; variant?: 'primary' | 'quiet'; icon?: IconName; type?: 'button' | 'submit' | 'reset'; disabled?: boolean }) { return <button type={type} disabled={disabled} className={`button button-${variant}`} onClick={onClick}>{children}{icon && <Icon name={icon} size={16} />}</button>; }
function SectionHeading({ eyebrow, title, children }: { eyebrow?: string; title: string; children?: React.ReactNode }) { return <div className="section-heading">{eyebrow && <Eyebrow>{eyebrow}</Eyebrow>}<h2>{title}</h2>{children && <p>{children}</p>}</div>; }
function RouteBack() {
  const current = route();
  const destinations = {
    '/analyze': { to: '/', label: 'Back to home' },
    '/mapping': { to: '/', label: 'Back to home' },
    '/mapping/confirm': { to: '/mapping/review', label: 'Back to review' },
    '/report': { to: '/analyze', label: 'Back to analyze' },
  };
  const destination = destinations[current];
  if (!destination) return null;
  return <div className="route-utility"><button onClick={() => go(destination.to)}><span>←</span>{destination.label}</button></div>;
}

function Home() {
  return <div className="home-page page-pad">
    <section className="hero-grid">
      <div className="hero-copy">
        <Eyebrow>Balance intelligence · 01</Eyebrow>
        <h1>Frame the patch<br /><em>before you ship.</em></h1>
        <p className="hero-lede">Patch Brief turns raw player signals, design intent, and community noise into a decision-ready brief for your next update.</p>
        <div className="hero-actions"><Button onClick={() => go('/analyze')} icon="arrow">Start an analysis</Button><Button variant="quiet" onClick={() => go('/mapping')}>Set up data mapping</Button></div>
        <div className="hero-note"><span className="signal-pip"></span><span>Built for designers who need the whole picture, not another dashboard.</span></div>
      </div>
      <div className="hero-art" aria-label="Balance signal visualization">
        <div className="orbit orbit-one"></div><div className="orbit orbit-two"></div><div className="orbit orbit-three"></div>
        <div className="balance-core"><span className="core-label">LIVE<br />SIGNAL</span><strong>38<span>%</span></strong><span className="core-foot">low-bracket impact</span></div>
        <div className="signal-tag tag-one"><span className="tag-dot mint"></span>playtest / live <b>aligned</b></div>
        <div className="signal-tag tag-two"><span className="tag-dot coral"></span>community / data <b>divergent</b></div>
        <div className="signal-tag tag-three"><span className="tag-dot yellow"></span>risk index <b>high</b></div>
      </div>
    </section>
    <section className="home-strip"><div><span className="strip-number">01</span><strong>Bring the evidence</strong><span>Telemetry, playtests, rules, and player voice.</span></div><div><span className="strip-number">02</span><strong>Find the tension</strong><span>See where data and perception split.</span></div><div><span className="strip-number">03</span><strong>Make the call</strong><span>Leave the final lever with the designer.</span></div></section>
    <section className="home-lower"><SectionHeading eyebrow="The brief, not the black box" title="A clearer read on what changes." /><div className="feature-grid"><Feature number="A" title="Affected players" text="Cohorts, entities, and the actual size of the blast radius." color="mint" /><Feature number="B" title="Second-order risks" text="Stakeholder conflict, meta shifts, and identity levers before they surprise you." color="coral" /><Feature number="C" title="Decision paths" text="Several defensible ways forward — clearly labeled for designer judgment." color="yellow" /></div></section>
  </div>;
}
function Feature({ number, title, text, color }) { return <article className={`feature-card ${color}`}><div className="feature-top"><span>{number}</span><Icon name="arrow" size={15} /></div><h3>{title}</h3><p>{text}</p></article>; }

function FileRow({ file, value, onChange }) {
  const input = useRef<HTMLInputElement>(null);
  return <div className={`file-row ${value ? 'ready' : ''}`} onClick={() => input.current?.click()} role="button" tabIndex={0} onKeyDown={(e) => e.key === 'Enter' && input.current?.click()}>
    <input ref={input} type="file" accept=".json,application/json" onChange={(e) => onChange(e.target.files?.[0])} />
    <span className="file-icon"><Icon name={value ? 'check' : 'upload'} size={17} /></span><span className="file-name"><strong>{file.label}</strong><small>{value ? `${(value.size / 1024).toFixed(1)} KB · ready to send` : file.description}</small></span><span className="file-state">{value ? 'Ready' : file.optional ? 'Optional' : 'Missing'}</span><Icon name="chevron" size={16} />
  </div>;
}

function Analyze() {
  const [files, setFiles] = useState({});
  const [adapters, setAdapters] = useState([{ adapter_id: 'demo_moba', created_at: '2026-07-11T08:00:00Z' }]);
  const [adapterId, setAdapterId] = useState('demo_moba');
  const [error, setError] = useState('');
  const allRequired = requiredFiles.filter((f) => !f.optional).every((f) => files[f.key]);
  useEffect(() => { if (hasApi) listAdapters().then((data) => { if (data.adapters?.length) { setAdapters(data.adapters); setAdapterId(data.adapters[0].adapter_id); } }).catch(() => {}); }, []);
  const demo = () => { const next = {}; requiredFiles.forEach((file) => { next[file.key] = new File(['{ "demo": true }'], file.label, { type: 'application/json' }); }); setFiles(next); setError(''); };
  const updateFile = (key, file) => { if (!file) return; if (!file.name.toLowerCase().endsWith('.json')) { setError(`${file.name} is not a JSON file.`); return; } setFiles((prev) => ({ ...prev, [key]: file })); setError(''); };
  const submit = async () => { setError(''); sessionStorage.removeItem('patchBriefReport'); sessionStorage.setItem('patchBriefStartedAt', String(Date.now())); go('/analyze/loading'); try { const report = hasApi ? await runAnalysis(files, adapterId) : demoReport; sessionStorage.setItem('patchBriefReport', JSON.stringify(report)); } catch (e) { sessionStorage.setItem('patchBriefReport', JSON.stringify(demoReport)); sessionStorage.setItem('patchBriefError', e.message); } };
  return <div className="page-pad narrow-page"><div className="page-intro split-intro"><div><Eyebrow>New analysis · input room</Eyebrow><h1>What are you<br /><em>about to change?</em></h1></div><p>Bring the evidence behind the patch. We’ll line it up against player behavior, design rules, and community signal.</p></div>
    <section className="analysis-panel"><div className="panel-head"><div><span className="panel-kicker">01 / Adapter</span><h2>Choose the lens</h2></div><button className="text-link" onClick={() => go('/mapping')}>Create new mapping <Icon name="arrow" size={14} /></button></div><div className="select-wrap"><select value={adapterId} onChange={(e) => setAdapterId(e.target.value)}>{adapters.map((adapter) => <option key={adapter.adapter_id} value={adapter.adapter_id}>{adapter.adapter_id === 'demo_moba' ? 'Demo adapter · MOBA' : adapter.adapter_id}</option>)}</select><Icon name="chevron" size={16} /></div></section>
    <section className="analysis-panel files-panel"><div className="panel-head"><div><span className="panel-kicker">02 / Evidence bundle</span><h2>Load the source files</h2></div><button className="demo-link" onClick={demo}><Icon name="spark" size={15} /> Load demo data</button></div><div className="file-list">{requiredFiles.map((file) => <FileRow key={file.key} file={file} value={files[file.key]} onChange={(value) => updateFile(file.key, value)} />)}</div>{error && <div className="error-banner">{error}</div>}<div className="files-foot"><span><span className="ready-count">{Object.keys(files).length}</span> / 5 required files ready</span><span>JSON only · max 25 MB each</span></div></section>
    <div className="submit-row"><span className="submit-hint">{allRequired ? 'Your evidence bundle is ready.' : 'Load required files or use demo data to continue.'}</span><Button disabled={!allRequired} onClick={submit} icon="arrow">Run analysis</Button></div>
  </div>;
}

function Loading() {
  const [step, setStep] = useState(0);
  const steps = ['Ingest & normalize', 'Metrics & cohorts', 'Context merge', 'Impact & risk', 'Generating brief'];
  useEffect(() => {
    const timer = setInterval(() => setStep((value) => Math.min(value + 1, steps.length - 1)), 1100);
    let waitTimer: number | undefined;
    const waitForReport = () => {
      if (sessionStorage.getItem('patchBriefReport')) { go('/report'); return; }
      const startedAt = Number(sessionStorage.getItem('patchBriefStartedAt') || Date.now());
      if (Date.now() - startedAt > 60000) { sessionStorage.setItem('patchBriefError', 'Analysis timed out. Please try again.'); go('/error'); return; }
      waitTimer = window.setTimeout(waitForReport, 300);
    };
    const done = window.setTimeout(waitForReport, 5600);
    return () => { clearInterval(timer); window.clearTimeout(done); if (waitTimer) window.clearTimeout(waitTimer); };
  }, []);
  return <div className="loading-page page-pad"><div className="loading-center"><div className="loading-orbit"><span></span><span></span><span></span><div><Icon name="pulse" size={29} /></div></div><Eyebrow>Pipeline in motion</Eyebrow><h1>Reading the signal<br /><em>behind the patch.</em></h1><p>Eight layers are being assembled into one decision-ready brief.</p><div className="progress-track"><span style={{ width: `${((step + 1) / steps.length) * 100}%` }}></span></div><div className="pipeline-list">{steps.map((item, index) => <div key={item} className={index < step ? 'done' : index === step ? 'current' : ''}><span className="pipeline-check">{index < step ? <Icon name="check" size={13} /> : String(index + 1).padStart(2, '0')}</span><span>{item}</span>{index === step && <small>working</small>}</div>)}</div></div></div>;
}

function ConfidenceBadge({ value }) { const label = value >= 0.9 ? 'High' : value >= 0.7 ? 'Medium' : 'Low'; return <span className={`confidence ${label.toLowerCase()}`}><i></i>{Math.round(value * 100)}% · {label}</span>; }
function StepIndicator({ current }) { return <div className="step-indicator">{['Upload', 'Review', 'Confirm'].map((label, index) => <div key={label} className={index + 1 <= current ? 'active' : ''}><span>{index + 1}</span>{label}</div>)}</div>; }

function MappingUpload() {
  const [file, setFile] = useState(null); const [genre, setGenre] = useState('MOBA'); const input = useRef<HTMLInputElement>(null);
  const choose = (value: File | undefined) => { if (value?.name.endsWith('.json')) { mappingSampleFile = value; setFile(value); } };
  const next = async () => { if (!file) return; let suggestion: any = mappingRows; if (hasApi) { try { suggestion = await suggestMapping(file, genre); } catch { /* keep local fallback */ } } sessionStorage.setItem('mappingRows', JSON.stringify(suggestion.field_map ? [...suggestion.field_map.map((x) => ({ ...x, confidence: x.confidence || 0.8, kind: 'Field', sample: '—' })), ...(suggestion.event_map || []).map((x) => ({ ...x, source: x.source, target: x.target, confidence: x.confidence || 0.8, kind: 'Event', sample: x.source }))] : suggestion)); sessionStorage.setItem('mappingFile', file.name); go('/mapping/review'); };
  return <div className="page-pad narrow-page"><div className="mapping-top"><div><Eyebrow>Layer 0 · Adapter setup</Eyebrow><h1>Map your data<br /><em>to our schema.</em></h1><p>One small translation layer lets every future analysis speak the same language.</p></div><StepIndicator current={1} /></div><section className="mapping-upload-card"><label className="field-label" htmlFor="genre">Optional context</label><div className="select-wrap"><select id="genre" value={genre} onChange={(e) => setGenre(e.target.value)}><option>MOBA</option><option>FPS</option><option>RPG</option><option>Other</option></select><Icon name="chevron" size={16} /></div><label className="field-label upload-label">Sample log</label><div className={`dropzone ${file ? 'has-file' : ''}`} onClick={() => input.current?.click()} onDragOver={(e) => e.preventDefault()} onDrop={(e) => { e.preventDefault(); choose(e.dataTransfer.files?.[0]); }}><input ref={input} type="file" accept=".json,application/json" onChange={(e) => choose(e.target.files?.[0])} />{file ? <><span className="drop-icon ready"><Icon name="check" size={22} /></span><strong>{file.name}</strong><small>{(file.size / 1024).toFixed(1)} KB · ready to map</small><button className="replace-link" onClick={(e) => { e.stopPropagation(); setFile(null); }}>Replace file</button></> : <><span className="drop-icon"><Icon name="upload" size={22} /></span><strong>Drop your sample JSON here</strong><small>or click to browse · one representative log is enough</small></>}</div><div className="mapping-card-foot"><span>We never modify your source file.</span><Button disabled={!file} onClick={next} icon="arrow">Suggest mapping</Button></div></section></div>;
}

function MappingReview() {
  const [rows, setRows] = useState(() => JSON.parse(sessionStorage.getItem('mappingRows') || JSON.stringify(mappingRows))); const [tab, setTab] = useState('All'); const [preview, setPreview] = useState({ events_parsed: 1240, events_skipped: 12, warnings: ['12 rows missing required field: t'] });
  const fileName = sessionStorage.getItem('mappingFile') || 'sample_logs.json';
  const filtered = rows.filter((row) => tab === 'All' || row.kind === tab);
  const update = (source, target) => setRows((value) => value.map((row) => row.source === source ? { ...row, target } : row));
  const refreshPreview = async () => { if (hasApi) { try { const data = await previewMapping(mappingSampleFile || new File(['{}'], fileName, { type: 'application/json' }), rows.filter((x) => x.kind === 'Field'), rows.filter((x) => x.kind === 'Event')); setPreview(data); } catch { /* local preview remains useful */ } } };
  const next = () => { sessionStorage.setItem('mappingRows', JSON.stringify(rows)); go('/mapping/confirm'); };
  return <div className="page-pad review-page"><div className="mapping-top"><div><Eyebrow>Layer 0 · Review suggestions</Eyebrow><h1>Make the translation<br /><em>feel right.</em></h1><p>We flagged the uncertain rows. Everything else is ready to carry forward.</p></div><StepIndicator current={2} /></div><div className="review-layout"><section className="mapping-table-card"><div className="table-toolbar"><div className="tabs">{['All', 'Field', 'Event'].map((item) => <button key={item} className={tab === item ? 'active' : ''} onClick={() => setTab(item)}>{item} map</button>)}</div><span className="file-chip"><Icon name="check" size={13} /> {fileName}</span></div><div className="mapping-table"><div className="table-row table-header"><span>Source field</span><span>Sample value</span><span>Canonical target</span><span>Confidence</span></div>{filtered.map((row) => <div className="table-row" key={row.source}><span className="source-cell"><span className="type-mark">{row.kind === 'Event' ? 'EV' : 'FL'}</span><strong>{row.source}</strong></span><span className="sample-value">{row.sample}</span><label className="target-select"><select value={row.target} onChange={(e) => update(row.source, e.target.value)}>{canonicalOptions.map((item) => <option key={item}>{item}</option>)}</select><Icon name="chevron" size={14} /></label><ConfidenceBadge value={row.confidence} /></div>)}</div><div className="table-foot"><span>Targets come from the canonical schema · {rows.length} rows</span><button className="text-link" onClick={refreshPreview}>Refresh preview <Icon name="pulse" size={14} /></button></div></section><aside className="preview-card"><div className="preview-card-head"><span className="panel-kicker">Live preview</span><span className="live-label"><i></i>sample run</span></div><div className="preview-number"><strong>{preview.events_parsed.toLocaleString()}</strong><span>events parsed</span></div><div className="preview-split"><div><strong>{preview.events_skipped}</strong><span>skipped</span></div><div><strong>{preview.warnings?.length || 0}</strong><span>warnings</span></div></div><div className="warning-list">{preview.warnings?.map((warning) => <div key={warning}><span>!</span>{warning}</div>)}</div><div className="preview-sample"><span className="panel-kicker">Sample output</span><code>{'{ type: "death", t: 45.1 }'}</code></div></aside></div><div className="submit-row review-submit"><Button variant="quiet" onClick={() => go('/mapping')}>Back</Button><Button onClick={next} icon="arrow">Continue to confirm</Button></div></div>;
}

function MappingConfirm() {
  const rows = JSON.parse(sessionStorage.getItem('mappingRows') || JSON.stringify(mappingRows)); const [name, setName] = useState('studio_a_moba'); const [defaultAdapter, setDefaultAdapter] = useState(true); const [saved, setSaved] = useState(false);
  const save = async () => { const payload = { adapter_id: name, canonical_version: '1.0', field_map: Object.fromEntries(rows.filter((x) => x.kind !== 'Event').map((x) => [x.source, x.target])), event_map: Object.fromEntries(rows.filter((x) => x.kind === 'Event').map((x) => [x.source, x.target])), custom_fields: {}, confirmed_by_user: true }; if (hasApi) { try { await confirmMapping(payload); } catch { /* local save keeps the demo flow available */ } } if (defaultAdapter) localStorage.setItem('patchBriefDefaultAdapter', name); setSaved(true); setTimeout(() => go('/analyze'), 700); };
  return <div className="page-pad narrow-page"><div className="mapping-top"><div><Eyebrow>Layer 0 · Save adapter</Eyebrow><h1>Name the lens<br /><em>you’ll reuse.</em></h1><p>This mapping stays local to your workspace until you choose to send it to the backend.</p></div><StepIndicator current={3} /></div><section className="confirm-card"><div className="confirm-summary"><div className="confirm-summary-head"><span className="panel-kicker">Final mapping</span><span className="summary-count">{rows.length} translations</span></div>{rows.slice(0, 5).map((row) => <div className="summary-row" key={row.source}><span>{row.source}</span><Icon name="arrow" size={13} /><strong>{row.target}</strong></div>)}</div><div className="confirm-form"><label className="field-label" htmlFor="adapter-name">Adapter name</label><input id="adapter-name" value={name} onChange={(e) => setName(e.target.value.toLowerCase().replace(/[^a-z0-9_]/g, '_'))} /><label className="check-control"><input type="checkbox" checked={defaultAdapter} onChange={(e) => setDefaultAdapter(e.target.checked)} /><span className="fake-check"><Icon name="check" size={12} /></span>Set as default for this browser session</label><Button disabled={!name || saved} onClick={save} icon={saved ? 'check' : 'arrow'}>{saved ? 'Adapter saved' : 'Save adapter'}</Button></div></section></div>;
}

function RiskBadge({ severity }) { return <span className={`risk-badge ${severity}`}>{severity} risk</span>; }
function Report() {
  const report = useMemo(() => JSON.parse(sessionStorage.getItem('patchBriefReport') || JSON.stringify(demoReport)), []); const [copied, setCopied] = useState(false); const copy = async () => { await navigator.clipboard?.writeText(report.report_markdown || report.executive_summary); setCopied(true); setTimeout(() => setCopied(false), 1400); };
  return <div className="report-page"><section className="report-hero"><div className="report-hero-inner"><div><Eyebrow>Impact brief · {report.report_id || 'demo'}</Eyebrow><h1>Ironclad /<br /><em>the readout.</em></h1><p>{report.executive_summary}</p><div className="report-meta"><span>Generated 11 Jul 2026</span><span className="meta-divider"></span><span className="ai-status"><i></i>{report.llm_used ? 'LLM assisted' : 'Template fallback'}</span></div></div><div className="report-risk-lockup"><span>Overall exposure</span><strong>High</strong><RiskBadge severity="high" /></div></div></section><div className="report-body"><div className="report-main"><section id="section-0" className="report-section affected-section"><div className="report-section-head"><div><span className="section-index">01</span><h2>Who is affected</h2></div><span className="section-note">3 cohorts in frame</span></div><div className="affected-table"><div className="affected-row affected-head"><span>Entity / role</span><span>Cohort</span><span>Impact</span><span>Signals</span></div>{report.who_is_affected.map((item) => <div className="affected-row" key={`${item.entity_id}-${item.cohort}`}><span><strong>{item.entity_name}</strong><small>{item.entity_id} · {item.role}</small></span><span>{item.cohort}</span><span><span className={`impact-dot ${item.impact}`}></span>{item.impact}</span><span className="signal-values">{item.metric_refs.map((ref) => <small key={ref}>{ref}</small>)}</span></div>)}</div></section><section id="section-1" className="report-section"><div className="report-section-head"><div><span className="section-index">02</span><h2>Proposed changes</h2></div><span className="section-note">from update plan</span></div><div className="change-list">{report.proposed_changes.map((item) => <div className="change-row" key={`${item.entity_id}-${item.field}`}><span className="change-entity"><strong>{item.entity_name}</strong><small>{item.role} · {item.target}</small></span><span className="change-field">{item.field}</span><span className="change-values"><b>{item.from}</b><Icon name="arrow" size={14} /><b>{item.to}</b></span><span className="delta">{item.delta}</span></div>)}</div></section><section id="section-2" className="report-section alignment-section"><div className="report-section-head"><div><span className="section-index">03</span><h2>Where signals disagree</h2></div><span className="section-note">alignment read</span></div><div className="alignment-grid"><div className="alignment-card divergence"><span>Data vs community</span><strong>{report.alignment.data_vs_community}</strong><small>Player voice is louder than the raw win-rate story.</small></div><div className="alignment-card aligned"><span>Playtest vs live</span><strong>{report.alignment.playtest_vs_live}</strong><small>The current test environment reflects live behavior.</small></div></div><div className="pattern-list">{report.alignment.patterns.map((pattern) => <div className="pattern-row" key={pattern.id}><span className="pattern-arrow">↗</span><div><strong>{pattern.title}</strong><span>{pattern.description}</span></div><span className={`confidence-word ${pattern.confidence}`}>{pattern.confidence}</span></div>)}</div></section><section id="section-3" className="report-section risks-section"><div className="report-section-head"><div><span className="section-index">04</span><h2>Risks to carry forward</h2></div><span className="section-note">before you lock the patch</span></div><div className="risk-grid">{report.risks.map((risk) => <article className={`risk-card ${risk.severity}`} key={risk.id}><div className="risk-card-top"><RiskBadge severity={risk.severity} /><span>↗</span></div><h3>{risk.title}</h3><ul>{risk.evidence.map((item) => <li key={item}>{item}</li>)}</ul></article>)}</div></section><section id="section-4" className="report-section solutions-section"><div className="report-section-head"><div><span className="section-index">05</span><h2>Paths, not prescriptions</h2></div><span className="section-note">designer decides</span></div><div className="solution-list">{report.solution_paths.map((path, index) => <div className="solution-row" key={path.type}><span className="solution-number">0{index + 1}</span><div><strong>{path.label}</strong><span>{path.rationale}</span></div><span className={`solution-confidence ${path.confidence}`}>{path.confidence}</span></div>)}</div></section><section id="section-5" className="report-section validation-section"><div className="report-section-head"><div><span className="section-index">06</span><h2>Validation plan</h2></div></div><div className="validation-list">{report.validation_plan.map((item, index) => <div key={item}><span>{String(index + 1).padStart(2, '0')}</span><strong>{item}</strong><Icon name="check" size={16} /></div>)}</div></section><section className="comms-block"><div><Eyebrow>Draft player comms</Eyebrow><p>“{report.draft_player_comms}”</p></div><button className="copy-button" onClick={copy}><Icon name={copied ? 'check' : 'copy'} size={15} />{copied ? 'Copied' : 'Copy draft'}</button></section></div><aside className="report-rail"><div className="rail-sticky"><div className="rail-label">BRIEF INDEX</div>{['Who is affected', 'Proposed changes', 'Where signals disagree', 'Risks to carry forward', 'Paths, not prescriptions', 'Validation plan'].map((label, index) => <a href={`#section-${index}`} key={label}><span>0{index + 1}</span>{label}</a>)}<div className="rail-divider"></div><Button variant="quiet" onClick={() => go('/analyze')} icon="arrow">New analysis</Button></div></aside></div></div>;
}

function ErrorPage() { return <div className="empty-page page-pad"><Eyebrow>Something is off</Eyebrow><h1>The signal<br /><em>got lost.</em></h1><p>Try the last action again, or return to the analysis room.</p><Button onClick={() => go('/analyze')} icon="arrow">Back to analyze</Button></div>; }

function App() {
  const [, rerender] = useState(0); const [health, setHealth] = useState<'demo' | 'online'>('demo');
  useEffect(() => { const listener = () => rerender((x) => x + 1); window.addEventListener('hashchange', listener); return () => window.removeEventListener('hashchange', listener); }, []);
  useEffect(() => { if (hasApi) getHealth().then(() => setHealth('online')).catch(() => setHealth('demo')); }, []);
  const current = route(); let page = current === '/' ? <Home /> : current === '/analyze' ? <Analyze /> : current === '/analyze/loading' ? <Loading /> : current === '/report' ? <Report /> : current === '/mapping' ? <MappingUpload /> : current === '/mapping/review' ? <MappingReview /> : current === '/mapping/confirm' ? <MappingConfirm /> : <ErrorPage />;
  return <AppShell health={health}><RouteBack />{page}</AppShell>;
}

createRoot(document.getElementById('root')).render(<React.StrictMode><App /></React.StrictMode>);
