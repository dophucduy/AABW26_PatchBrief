import { useEffect, useRef, useState, type CSSProperties, type RefObject } from 'react';
import gsap from 'gsap';
import { useGSAP } from '@gsap/react';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { Icon, type IconName } from './Ui';

gsap.registerPlugin(useGSAP, ScrollTrigger);

type LayerKind = 'tool' | 'guard' | 'ai' | 'out';

interface PipelineStep {
  id: string;
  name: string;
  kind: LayerKind;
  detail: string;
  does: string;
  icon: IconName;
  tags: string[];
}

const STEPS: PipelineStep[] = [
  {
    id: 'L0',
    name: 'Adapt',
    kind: 'tool',
    icon: 'scan',
    detail: 'Map studio field names to canonical telemetry schema.',
    does: 'Translates your studio JSON field names into the shared schema Patch Brief understands — so the same pipeline works across games.',
    tags: ['metric_map', 'adapter_id', 'field_alias'],
  },
  {
    id: 'L1',
    name: 'Ingest',
    kind: 'tool',
    icon: 'upload',
    detail: 'Validate structured rows — entity, bracket, rates in range.',
    does: 'Loads telemetry_live and telemetry_playtest files, checks types and ranges, and rejects malformed rows before any analysis runs.',
    tags: ['telemetry_live', 'telemetry_playtest', 'row_checks'],
  },
  {
    id: 'L2',
    name: 'Semantic',
    kind: 'tool',
    icon: 'database',
    detail: 'Join metrics with game definition — roles, stats, tags.',
    does: 'Links each telemetry row to game entities — roles, stats, tags — so metrics refer to real characters, items, or brackets.',
    tags: ['game_definition', 'entity_roles', 'stat_tags'],
  },
  {
    id: 'L3',
    name: 'Metrics',
    kind: 'tool',
    icon: 'chart',
    detail: 'Compute win rate, pick rate, and sessions per cohort.',
    does: 'Derives win rate, pick rate, and session counts per cohort from structured data — numbers the brief can cite, not LLM guesses.',
    tags: ['win_rate', 'pick_rate', 'sessions'],
  },
  {
    id: 'L4',
    name: 'Context',
    kind: 'tool',
    icon: 'layers',
    detail: 'Merge rules, update plan, and community into one bundle.',
    does: 'Bundles rules.json, update_plan, and community signal into one context object the guardrails and report can read together.',
    tags: ['rules.json', 'update_plan', 'community'],
  },
  {
    id: 'L5',
    name: 'Impact',
    kind: 'guard',
    icon: 'warning',
    detail: 'Rule engine: bracket splits, perception vs data, plan conflicts.',
    does: 'Runs deterministic rules — bracket splits, data vs community divergence, plan conflicts — and attaches evidence for each pattern found.',
    tags: ['pattern_match', 'cohort_split', 'alignment'],
  },
  {
    id: 'L6',
    name: 'Risk',
    kind: 'guard',
    icon: 'shield',
    detail: 'Map patterns to risks, solution paths, and validation steps.',
    does: 'Turns L5 patterns into ranked risks, testable solution paths, and a validation plan designers can act on before shipping.',
    tags: ['risk_frame', 'solution_paths', 'validation'],
  },
  {
    id: 'L7',
    name: 'Report',
    kind: 'ai',
    icon: 'brain',
    detail: 'Balance agent writes the brief from proven evidence only.',
    does: 'The balance agent narrates the decision brief from structured JSON only — executive summary, risks, and paths, with every claim traceable.',
    tags: ['grounded_llm', 'cited_evidence', 'no_hallucination'],
  },
  {
    id: 'OUT',
    name: 'Brief',
    kind: 'out',
    icon: 'book',
    detail: 'Executive summary, risks, paths, validation plan, draft comms.',
    does: 'The inspectable output your team reviews: who is affected, what could go wrong, what to test next — designer decides what ships.',
    tags: ['executive_summary', 'risks', 'validation_plan'],
  },
];

const KIND_LABEL: Record<LayerKind, string> = {
  tool: 'Tool',
  guard: 'Guardrail',
  ai: 'AI',
  out: 'Output',
};

const AUTO_CYCLE_MS = 5000;

export function PipelineFlow({ scrollerRef }: { scrollerRef?: RefObject<HTMLElement | null> }) {
  const rootRef = useRef<HTMLDivElement>(null);
  const lineFillRef = useRef<HTMLDivElement>(null);
  const detailRef = useRef<HTMLElement>(null);
  const iconRef = useRef<HTMLDivElement>(null);
  const [activeIndex, setActiveIndex] = useState(0);
  const [picked, setPicked] = useState(false);
  const [paused, setPaused] = useState(false);
  const active = STEPS[activeIndex];
  const progress = STEPS.length > 1 ? activeIndex / (STEPS.length - 1) : 0;

  const pickStep = (index: number) => {
    setPaused(true);
    setPicked(true);
    setActiveIndex(index);
    detailRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  };

  useEffect(() => {
    if (paused) return;
    const timer = window.setInterval(() => {
      setActiveIndex((current) => (current + 1) % STEPS.length);
    }, AUTO_CYCLE_MS);
    return () => window.clearInterval(timer);
  }, [paused]);

  useEffect(() => {
    const id = requestAnimationFrame(() => ScrollTrigger.refresh());
    return () => cancelAnimationFrame(id);
  }, []);

  useGSAP(
    () => {
      const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      const root = rootRef.current;
      const lineFill = lineFillRef.current;
      const scroller = scrollerRef?.current ?? undefined;
      if (!root || !lineFill) return;

      gsap.set('.pipeline-step-btn', { opacity: 1, scale: 1, clearProps: 'opacity,transform' });

      if (reduced) {
        gsap.set(lineFill, { scaleX: 1, scaleY: 1 });
        return;
      }

      const playLine = () => {
        const mm = window.matchMedia('(min-width: 721px)').matches;
        if (mm) {
          gsap.fromTo(
            lineFill,
            { scaleX: 0, scaleY: 1, transformOrigin: 'left center' },
            { scaleX: 1, duration: 1.1, ease: 'power2.out' },
          );
        } else {
          gsap.fromTo(
            lineFill,
            { scaleY: 0, scaleX: 1, transformOrigin: 'top center' },
            { scaleY: 1, duration: 1.1, ease: 'power2.out' },
          );
        }
      };

      const st = ScrollTrigger.create({
        trigger: root,
        scroller,
        start: 'top 90%',
        once: true,
        onEnter: playLine,
        onEnterBack: playLine,
      });

      requestAnimationFrame(() => {
        ScrollTrigger.refresh();
        const rect = root.getBoundingClientRect();
        const viewBottom = scroller
          ? scroller.getBoundingClientRect().bottom
          : window.innerHeight;
        if (rect.top < viewBottom * 0.92) playLine();
      });

      return () => {
        st.kill();
      };
    },
    { scope: rootRef },
  );

  useGSAP(
    () => {
      const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      const detail = detailRef.current;
      const icon = iconRef.current;
      if (!detail || reduced) return;

      gsap.fromTo(
        detail,
        { opacity: 0.5, y: 12 },
        { opacity: 1, y: 0, duration: 0.35, ease: 'power2.out' },
      );

      if (icon) {
        gsap.fromTo(
          icon,
          { scale: 0.85, rotate: -5 },
          { scale: 1, rotate: 0, duration: 0.45, ease: 'back.out(2)' },
        );
      }
    },
    { scope: rootRef, dependencies: [activeIndex] },
  );

  return (
    <div
      className="pipeline-milestone"
      ref={rootRef}
      aria-label="L0 through L7 pipeline"
      onMouseEnter={() => setPaused(true)}
    >
      <div className="x-container">
        <p className="pipeline-hint">
          {picked ? 'Showing layer detail — click another node to compare' : 'Click any layer to see what it does'}
        </p>

        <div className="pipeline-milestone-legend" aria-hidden="true">
          <span className="pipeline-legend-item pipeline-legend-tool">Tools L0–L4</span>
          <span className="pipeline-legend-item pipeline-legend-guard">Guardrails L5–L6</span>
          <span className="pipeline-legend-item pipeline-legend-ai">AI L7</span>
        </div>

        <div className="pipeline-rail-scroll">
          <div className="pipeline-rail">
            <div className="pipeline-line" aria-hidden="true">
              <div className="pipeline-line-fill" ref={lineFillRef} />
              <div
                className="pipeline-line-active"
                style={{ '--pipeline-progress': progress } as CSSProperties}
              />
            </div>

            <ol className="pipeline-steps" role="tablist" aria-label="Pipeline layers">
            {STEPS.map((step, index) => {
              const isActive = index === activeIndex;
              const isPast = index < activeIndex;
              return (
                <li
                  key={step.id}
                  className={`pipeline-step pipeline-step-${step.kind} ${isPast ? 'is-past' : ''}`}
                  role="presentation"
                >
                  <button
                    type="button"
                    role="tab"
                    id={`pipeline-tab-${step.id}`}
                    aria-selected={isActive}
                    aria-controls="pipeline-detail-panel"
                    className={`pipeline-step-btn ${isActive ? 'is-active' : ''}`}
                    onClick={() => pickStep(index)}
                  >
                    <span className="pipeline-node">
                      <span className="pipeline-node-ring" aria-hidden="true" />
                      <span className="pipeline-node-core">
                        <Icon name={step.icon} size={16} />
                      </span>
                    </span>
                    <span className="pipeline-step-meta">
                      <span className="pipeline-step-id">{step.id}</span>
                      <span className="pipeline-step-name">{step.name}</span>
                    </span>
                  </button>
                </li>
              );
            })}
          </ol>
          </div>
        </div>

        <article
          ref={detailRef}
          id="pipeline-detail-panel"
          role="tabpanel"
          aria-labelledby={`pipeline-tab-${active.id}`}
          className={`pipeline-detail-card pipeline-detail-${active.kind} pipeline-detail-step-${active.id} ${picked ? 'is-picked' : ''}`}
        >
          <div className="pipeline-detail-visual" ref={iconRef}>
            <div className={`pipeline-detail-icon pipeline-detail-icon-${active.kind}`}>
              <Icon name={active.icon} size={28} />
              <span className="pipeline-detail-pulse" aria-hidden="true" />
            </div>
            <div className="pipeline-detail-tags" aria-label="Layer signals">
              {active.tags.map((tag) => (
                <span key={tag}>{tag}</span>
              ))}
            </div>
            <div className="pipeline-detail-fx" aria-hidden="true">
              {active.kind === 'tool' && <span className="pipeline-fx-bars" />}
              {active.kind === 'guard' && <span className="pipeline-fx-scan" />}
              {active.kind === 'ai' && <span className="pipeline-fx-spark" />}
              {active.kind === 'out' && <span className="pipeline-fx-doc" />}
            </div>
          </div>
          <div className="pipeline-detail-copy">
            <header>
              <span className={`pipeline-detail-badge pipeline-detail-badge-${active.kind}`}>
                {KIND_LABEL[active.kind]}
              </span>
              <h3>
                {active.id} · {active.name}
              </h3>
            </header>
            <p className="pipeline-detail-does">{active.does}</p>
            <p className="pipeline-detail-tech">{active.detail}</p>
          </div>
        </article>
      </div>
    </div>
  );
}
