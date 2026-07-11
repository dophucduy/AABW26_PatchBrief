import { useEffect, useRef, useState, type ChangeEvent, type RefObject } from 'react';
import { canonicalOptions, mappingRows } from '../data';
import { confirmMapping, hasApi, previewMapping, suggestMapping } from '../api';
import type { AdapterSummary, MappingPreviewData, MappingRow, MappingStage } from '../types';
import { Button, Icon } from './Ui';

interface MappingDrawerProps {
  isOpen: boolean;
  initialStage?: MappingStage;
  triggerRef: RefObject<HTMLButtonElement | null>;
  onClose: () => void;
  onAdapterSaved: (adapter: AdapterSummary) => void;
}

const fallbackPreview: MappingPreviewData = {
  events_parsed: 1240,
  events_skipped: 12,
  warnings: ['12 rows are missing the timestamp field.'],
};

function normalizeSuggestion(suggestion: Awaited<ReturnType<typeof suggestMapping>>): MappingRow[] {
  const fields = (suggestion.field_map || []).map((row) => ({
    source: row.source,
    target: row.target,
    confidence: row.confidence || 0.8,
    kind: 'Field' as const,
    sample: 'Sample value',
  }));
  const events = (suggestion.event_map || []).map((row) => ({
    source: row.source,
    target: row.target,
    confidence: row.confidence || 0.8,
    kind: 'Event' as const,
    sample: row.source,
  }));
  return [...fields, ...events];
}

export function MappingDrawer({ isOpen, initialStage = 'upload', triggerRef, onClose, onAdapterSaved }: MappingDrawerProps) {
  const closeRef = useRef<HTMLButtonElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const drawerRef = useRef<HTMLElement>(null);
  const [stage, setStage] = useState<MappingStage>(initialStage);
  const [sampleFile, setSampleFile] = useState<File | undefined>();
  const [genre, setGenre] = useState('MOBA');
  const [rows, setRows] = useState<MappingRow[]>(mappingRows);
  const [tab, setTab] = useState<'All' | 'Field' | 'Event'>('All');
  const [preview, setPreview] = useState<MappingPreviewData>(fallbackPreview);
  const [adapterName, setAdapterName] = useState('studio_a_moba');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setStage(initialStage);
    const timer = window.setTimeout(() => closeRef.current?.focus(), 0);
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
      if (event.key !== 'Tab') return;
      const focusable = drawerRef.current?.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), select:not([disabled]), [href], [tabindex]:not([tabindex="-1"])');
      if (!focusable?.length) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.clearTimeout(timer);
      window.removeEventListener('keydown', handleKeyDown);
      triggerRef.current?.focus();
    };
  }, [initialStage, isOpen, onClose, triggerRef]);

  if (!isOpen) return null;

  const chooseFile = (file: File | undefined) => {
    if (!file) return;
    if (!file.name.toLowerCase().endsWith('.json')) {
      setError(`${file.name} is not a JSON file. Choose a .json sample.`);
      return;
    }
    setSampleFile(file);
    setError('');
  };

  const handleSampleChange = (event: ChangeEvent<HTMLInputElement>) => chooseFile(event.target.files?.[0]);

  const buildSuggestion = async () => {
    if (!sampleFile) return;
    setBusy(true);
    setError('');
    try {
      const suggestion = hasApi ? await suggestMapping(sampleFile, genre) : undefined;
      const nextRows = suggestion ? normalizeSuggestion(suggestion) : mappingRows;
      setRows(nextRows.length ? nextRows : mappingRows);
      setStage('review');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not suggest a mapping. You can still edit the local template.');
      setRows(mappingRows);
      setStage('review');
    } finally {
      setBusy(false);
    }
  };

  const updateTarget = (source: string, target: string) => {
    setRows((current) => current.map((row) => row.source === source ? { ...row, target } : row));
  };

  const refreshPreview = async () => {
    if (!sampleFile || !hasApi) {
      setPreview(fallbackPreview);
      return;
    }
    setBusy(true);
    try {
      const result = await previewMapping(sampleFile, rows.filter((row) => row.kind === 'Field'), rows.filter((row) => row.kind === 'Event'));
      setPreview(result);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not refresh preview. Showing local estimate.');
      setPreview(fallbackPreview);
    } finally {
      setBusy(false);
    }
  };

  const saveAdapter = async () => {
    const normalizedName = adapterName.trim().toLowerCase().replace(/[^a-z0-9_]/g, '_');
    if (!normalizedName) {
      setError('Enter an adapter name before saving.');
      return;
    }
    setBusy(true);
    setError('');
    const payload = {
      adapter_id: normalizedName,
      canonical_version: '1.0',
      field_map: Object.fromEntries(rows.filter((row) => row.kind === 'Field').map((row) => [row.source, row.target])),
      event_map: Object.fromEntries(rows.filter((row) => row.kind === 'Event').map((row) => [row.source, row.target])),
      custom_fields: {},
      confirmed_by_user: true,
    };
    try {
      if (hasApi) await confirmMapping(payload);
      localStorage.setItem('patchBriefDefaultAdapter', normalizedName);
      onAdapterSaved({ adapter_id: normalizedName, created_at: new Date().toISOString() });
      onClose();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Could not save adapter. Try again.');
    } finally {
      setBusy(false);
    }
  };

  const visibleRows = rows.filter((row) => tab === 'All' || row.kind === tab);
  const confidenceLabel = (confidence: number) => confidence >= 0.9 ? 'high' : confidence >= 0.7 ? 'medium' : 'low';

  return <div className="mapping-drawer-layer">
    <button className="mapping-scrim" onClick={onClose} aria-label="Close mapping builder"></button>
    <aside ref={drawerRef} className="mapping-drawer" role="dialog" aria-modal="true" aria-labelledby="mapping-drawer-title">
      <header className="drawer-header">
        <div><p className="eyebrow"><span></span>Layer 0 adapter</p><h2 id="mapping-drawer-title">Build the data translation</h2></div>
        <button ref={closeRef} className="icon-button" onClick={onClose} aria-label="Close mapping builder"><Icon name="close" /></button>
      </header>
      <ol className="drawer-steps" aria-label="Mapping progress">
        {(['upload', 'review', 'confirm'] as MappingStage[]).map((item, index) => <li key={item} className={stage === item ? 'active' : (['upload', 'review', 'confirm'].indexOf(stage) > index ? 'complete' : '')}><span>{index + 1}</span>{item}</li>)}
      </ol>

      {stage === 'upload' && <section className="drawer-content mapping-upload">
        <p>Upload one representative event log. The adapter stays within this analysis workspace.</p>
        <label className="field-label" htmlFor="mapping-genre">Game context</label>
        <select id="mapping-genre" value={genre} onChange={(event) => setGenre(event.target.value)}>
          <option>MOBA</option><option>FPS</option><option>RPG</option><option>Other</option>
        </select>
        <input ref={fileInputRef} id="mapping-sample-file" type="file" accept=".json,application/json" onChange={handleSampleChange} />
        <button className={`sample-dropzone ${sampleFile ? 'ready' : ''}`} onClick={() => fileInputRef.current?.click()}>
          <Icon name={sampleFile ? 'check' : 'upload'} size={24} />
          <strong>{sampleFile ? sampleFile.name : 'Choose a sample JSON file'}</strong>
          <span>{sampleFile ? `${(sampleFile.size / 1024).toFixed(1)} KB ready` : 'The source file is never modified.'}</span>
        </button>
        {error && <p className="form-error" role="alert">{error}</p>}
        <div className="drawer-actions"><Button variant="ghost" onClick={onClose}>Cancel</Button><Button disabled={!sampleFile || busy} onClick={buildSuggestion} icon="spark" iconAfter="arrow-right">{busy ? 'Preparing' : 'Suggest mapping'}</Button></div>
      </section>}

      {stage === 'review' && <section className="drawer-content mapping-review">
        <div className="drawer-review-head"><p>Correct only the uncertain rows. The source evidence remains attached to this workspace.</p><Button variant="outline" disabled={busy} onClick={refreshPreview} icon="pulse">Refresh preview</Button></div>
        <div className="mapping-tabs" role="tablist" aria-label="Mapping type">
          {(['All', 'Field', 'Event'] as const).map((item) => <button key={item} role="tab" aria-selected={tab === item} className={tab === item ? 'active' : ''} onClick={() => setTab(item)}>{item} map</button>)}
        </div>
        <div className="mapping-table-wrap">
          <table className="mapping-table"><thead><tr><th>Source</th><th>Sample</th><th>Canonical target</th><th>Confidence</th></tr></thead>
            <tbody>{visibleRows.map((row) => <tr key={`${row.kind}-${row.source}`}><td><span className="mapping-type">{row.kind === 'Event' ? 'EV' : 'FL'}</span>{row.source}</td><td>{row.sample}</td><td><select value={row.target} onChange={(event) => updateTarget(row.source, event.target.value)}>{canonicalOptions.map((option) => <option key={option} value={option}>{option}</option>)}</select></td><td><span className={`confidence ${confidenceLabel(row.confidence)}`}><i></i>{Math.round(row.confidence * 100)}%</span></td></tr>)}</tbody>
          </table>
        </div>
        <aside className="parse-preview"><div><span>Parsed</span><strong>{preview.events_parsed.toLocaleString()}</strong></div><div><span>Skipped</span><strong>{preview.events_skipped}</strong></div><div className="preview-warning"><Icon name="warning" size={16} /><span>{preview.warnings[0] || 'No warnings in this preview.'}</span></div></aside>
        {error && <p className="form-error" role="alert">{error}</p>}
        <div className="drawer-actions"><Button variant="ghost" onClick={() => setStage('upload')} icon="arrow-left">Back</Button><Button onClick={() => setStage('confirm')} iconAfter="arrow-right">Continue</Button></div>
      </section>}

      {stage === 'confirm' && <section className="drawer-content mapping-confirm">
        <p>Save the adapter and it will be selected for this analysis automatically.</p>
        <div className="mapping-summary">{rows.map((row) => <div key={`${row.kind}-${row.source}`}><span>{row.source}</span><Icon name="arrow-right" size={14} /><strong>{row.target}</strong></div>)}</div>
        <label className="field-label" htmlFor="adapter-name">Adapter name</label>
        <input id="adapter-name" value={adapterName} onChange={(event) => setAdapterName(event.target.value)} />
        {error && <p className="form-error" role="alert">{error}</p>}
        <div className="drawer-actions"><Button variant="ghost" onClick={() => setStage('review')} icon="arrow-left">Back</Button><Button disabled={busy} onClick={saveAdapter} icon="check">{busy ? 'Saving' : 'Save and select'}</Button></div>
      </section>}
    </aside>
  </div>;
}
