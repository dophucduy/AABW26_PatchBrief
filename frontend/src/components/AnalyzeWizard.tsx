import { useEffect, useMemo, useRef, useState } from 'react';
import type { RefObject } from 'react';
import { hasApi } from '../api';
import { baseFiles, inputFiles } from '../data';
import type { AdapterSummary, AnalyzeFiles } from '../types';
import { Button, FileSlot, Icon } from './Ui';

type WizardStepId =
  | 'adapter'
  | 'telemetry'
  | 'update_plan'
  | 'game_definition'
  | 'rules'
  | 'community'
  | 'review';

interface WizardStep {
  id: WizardStepId;
  title: string;
  lead: string;
  fileKey?: string;
}

function stepHeading(index: number, title: string) {
  return `Step ${index + 1}: ${title}`;
}

const WIZARD_STEPS: WizardStep[] = [
  {
    id: 'adapter',
    title: 'Choose your mapping',
    lead: 'Pick a saved field mapping, or create one if your JSON uses custom keys.',
  },
  {
    id: 'telemetry',
    title: 'Upload telemetry',
    lead: 'Win rates, pick rates, and session counts by bracket — the live signal for this run.',
    fileKey: 'telemetry',
  },
  {
    id: 'update_plan',
    title: 'Upload update plan',
    lead: 'The proposed balance changes you want evaluated before shipping.',
    fileKey: 'update_plan',
  },
  {
    id: 'game_definition',
    title: 'Upload game definition',
    lead: 'Entity roster, stats, roles, and bracket thresholds the brief is grounded on.',
    fileKey: 'game_definition',
  },
  {
    id: 'rules',
    title: 'Upload balance rules',
    lead: 'Which levers are locked vs open for each entity — guardrails for recommendations.',
    fileKey: 'rules',
  },
  {
    id: 'community',
    title: 'Enter game name',
    lead: 'We scrape recent Steam reviews for this title and merge them into the brief.',
  },
  {
    id: 'review',
    title: 'Review and run analysis',
    lead: 'Confirm everything looks right, then run the full L0–L7 pipeline.',
  },
];

const FILE_META = [...inputFiles, ...baseFiles].reduce<Record<string, { label: string; description: string }>>(
  (acc, file) => {
    acc[file.key] = { label: file.label, description: file.description };
    return acc;
  },
  {},
);

function stepIndex(id: WizardStepId) {
  return WIZARD_STEPS.findIndex((step) => step.id === id);
}

export interface AnalyzeWizardProps {
  files: AnalyzeFiles;
  gameName: string;
  adapters: AdapterSummary[];
  adapterId: string;
  error: string;
  submitting: boolean;
  loadingDemo: boolean;
  mappingTriggerRef: RefObject<HTMLButtonElement | null>;
  onAdapterChange: (adapterId: string) => void;
  onGameNameChange: (value: string) => void;
  onSelectFile: (key: string, file: File) => void;
  onOpenMapping: () => void;
  onLoadDemo: () => void;
  onRunDemo: () => void;
  onRunAnalysis: () => void;
  jumpToReviewToken?: number;
}

export function AnalyzeWizard({
  files,
  gameName,
  adapters,
  adapterId,
  error,
  submitting,
  loadingDemo,
  mappingTriggerRef,
  onAdapterChange,
  onGameNameChange,
  onSelectFile,
  onOpenMapping,
  onLoadDemo,
  onRunDemo,
  onRunAnalysis,
  jumpToReviewToken = 0,
}: AnalyzeWizardProps) {
  const [activeStep, setActiveStep] = useState(0);
  const stepPanelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (jumpToReviewToken > 0) {
      setActiveStep(WIZARD_STEPS.length - 1);
    }
  }, [jumpToReviewToken]);

  const isStepComplete = (step: WizardStep): boolean => {
    if (step.id === 'adapter') return Boolean(adapterId);
    if (step.id === 'community') return !hasApi || gameName.trim().length > 0;
    if (step.id === 'review') {
      return WIZARD_STEPS.slice(0, -1).every((item) => {
        if (item.id === 'adapter') return Boolean(adapterId);
        if (item.id === 'community') return !hasApi || gameName.trim().length > 0;
        if (item.fileKey) return Boolean(files[item.fileKey]);
        return false;
      });
    }
    if (step.fileKey) return Boolean(files[step.fileKey]);
    return false;
  };

  const stepStates = useMemo(
    () => WIZARD_STEPS.map((step, index) => ({
      step,
      index,
      complete: isStepComplete(step),
      current: index === activeStep,
    })),
    [activeStep, adapterId, files, gameName],
  );

  const current = WIZARD_STEPS[activeStep];
  const canContinue = isStepComplete(current);
  const allReady = isStepComplete(WIZARD_STEPS[WIZARD_STEPS.length - 1]);
  const progress = Math.round(
    (stepStates.filter((item) => item.complete).length / WIZARD_STEPS.length) * 100,
  );

  const goToStep = (index: number) => {
    setActiveStep(index);
    stepPanelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  };

  const goNext = () => {
    if (!canContinue || activeStep >= WIZARD_STEPS.length - 1) return;
    goToStep(activeStep + 1);
  };

  const goBack = () => {
    if (activeStep <= 0) return;
    goToStep(activeStep - 1);
  };

  const handleFileSelect = (key: string, file: File) => {
    onSelectFile(key, file);
    const step = WIZARD_STEPS[activeStep];
    if (step.fileKey === key && activeStep < WIZARD_STEPS.length - 1) {
      window.setTimeout(() => goToStep(activeStep + 1), 320);
    }
  };

  return (
    <div className="wizard-layout">
      <aside className="wizard-rail" aria-label="Analysis steps">
        <div className="wizard-rail-head">
          <span className="wizard-rail-kicker">Step-by-step</span>
          <strong>{progress}% ready</strong>
          <div className="wizard-progress" aria-hidden="true">
            <span style={{ width: `${progress}%` }} />
          </div>
        </div>
        <ol className="wizard-rail-list">
          {stepStates.map(({ step, index, complete, current: isCurrent }) => (
            <li key={step.id}>
              <button
                type="button"
                className={`wizard-rail-item ${isCurrent ? 'current' : ''} ${complete ? 'complete' : ''}`}
                onClick={() => goToStep(index)}
                aria-current={isCurrent ? 'step' : undefined}
              >
                <span className="wizard-rail-marker">
                  {complete ? <Icon name="check" size={14} /> : index + 1}
                </span>
                <span className="wizard-rail-copy">
                  <b>{stepHeading(index, step.title)}</b>
                  <small>{complete ? 'Done' : isCurrent ? 'Current step' : 'Up next'}</small>
                </span>
              </button>
            </li>
          ))}
        </ol>
        <div className="wizard-rail-shortcuts">
          <Button variant="ghost" onClick={() => void onLoadDemo()} icon="spark" disabled={loadingDemo || !hasApi}>
            {loadingDemo ? 'Loading…' : 'Load demo files'}
          </Button>
          <Button variant="outline" onClick={onRunDemo} icon="pulse" disabled={submitting || !hasApi}>
            Skip to live demo
          </Button>
        </div>
      </aside>

      <section className="wizard-panel" ref={stepPanelRef} aria-labelledby="wizard-step-title">
        <header className="wizard-panel-head">
          <h2 id="wizard-step-title">{stepHeading(activeStep, current.title)}</h2>
          <p>{current.lead}</p>
        </header>

        <div className="wizard-panel-body">
          {current.id === 'adapter' && (
            <div className="wizard-card">
              <label className="select-field wizard-select">
                <span>Saved mapping</span>
                <select value={adapterId} onChange={(event) => onAdapterChange(event.target.value)}>
                  {adapters.map((adapter) => (
                    <option key={adapter.adapter_id} value={adapter.adapter_id}>
                      {adapter.adapter_id === 'demo_moba' ? 'Demo adapter — MOBA' : adapter.adapter_id}
                    </option>
                  ))}
                </select>
                <Icon name="chevron-down" size={16} />
              </label>
              <p className="wizard-hint">Use the demo adapter for the bundled MOBA fixtures, or create a custom mapping for your studio JSON.</p>
              <Button ref={mappingTriggerRef} variant="outline" onClick={onOpenMapping} icon="plus">
                Create new mapping
              </Button>
            </div>
          )}

          {current.fileKey && (
            <div className="wizard-upload-focus">
              <FileSlot
                id={`wizard-file-${current.fileKey}`}
                label={FILE_META[current.fileKey]?.label ?? `${current.fileKey}.json`}
                description={FILE_META[current.fileKey]?.description ?? 'Upload a JSON file'}
                file={files[current.fileKey]}
                onSelect={(file) => handleFileSelect(current.fileKey!, file)}
              />
              <p className="wizard-hint">JSON only · max 25 MB · one file for this step</p>
            </div>
          )}

          {current.id === 'community' && (
            <div className="wizard-card">
              <label className="field-label" htmlFor="wizard-game-name">Game name</label>
              <input
                id="wizard-game-name"
                className="text-input wizard-text-input"
                type="text"
                value={gameName}
                onChange={(event) => onGameNameChange(event.target.value)}
                placeholder="e.g. Dota 2"
                autoComplete="off"
              />
              <p className="wizard-hint">Steam reviews are fetched during analysis. Without an Apify token, fixture community data is used instead.</p>
            </div>
          )}

          {current.id === 'review' && (
            <div className="wizard-review">
              <ul className="wizard-review-list">
                {WIZARD_STEPS.slice(0, -1).map((step, index) => (
                  <li key={step.id} className={isStepComplete(step) ? 'ready' : 'missing'}>
                    <span>{stepHeading(index, step.title)}</span>
                    <b>
                      {step.id === 'adapter'
                        ? adapterId
                        : step.id === 'community'
                          ? gameName || '—'
                          : step.fileKey && files[step.fileKey]
                            ? files[step.fileKey]!.name
                            : 'Missing'}
                    </b>
                    <button type="button" className="wizard-review-edit" onClick={() => goToStep(stepIndex(step.id))}>
                      Edit
                    </button>
                  </li>
                ))}
              </ul>
              <p className="wizard-hint">
                {allReady
                  ? 'Evidence bundle complete. Running analysis will scrape Steam (if configured), then generate your brief.'
                  : 'Finish the missing steps before running analysis.'}
              </p>
            </div>
          )}

          {error && <p className="form-error" role="alert">{error}</p>}
        </div>

        <footer className="wizard-nav">
          <Button variant="ghost" onClick={goBack} icon="arrow-left" disabled={activeStep === 0}>
            Back
          </Button>
          {current.id === 'review' ? (
            <Button
              onClick={onRunAnalysis}
              icon="pulse"
              iconAfter="arrow-right"
              disabled={!allReady || submitting}
            >
              {submitting ? 'Starting analysis' : 'Run analysis'}
            </Button>
          ) : (
            <Button onClick={goNext} iconAfter="arrow-right" disabled={!canContinue}>
              Continue
            </Button>
          )}
        </footer>
      </section>
    </div>
  );
}
