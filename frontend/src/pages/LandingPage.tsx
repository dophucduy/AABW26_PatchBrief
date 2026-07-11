import { useEffect, useRef } from 'react';
import gsap from 'gsap';
import { useGSAP } from '@gsap/react';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { Button, Icon } from '../components/Ui';
import { GridParticles } from '../components/GridParticles';
import { LandingVideo } from '../components/LandingVideo';
import { PipelineFlow } from '../components/PipelineFlow';
import type { AppRoute } from '../types';

gsap.registerPlugin(useGSAP, ScrollTrigger);

const ABOUT_POINTS = [
  {
    title: 'What it is',
    body: 'A telemetry-grounded balance copilot for game studios. Upload structured JSON — live metrics, playtests, rules, patch plan, and community signal — get a decision brief before you ship.',
  },
  {
    title: 'Who it is for',
    body: 'Balance designers and live-ops leads who need evidence, not auto-patches. Patch Brief frames tradeoffs; your team decides what changes.',
  },
  {
    title: 'How it works',
    body: 'Eight deterministic layers (L0–L7) compute metrics, merge context, run guardrail rules, then an AI agent narrates the brief from proven JSON only.',
  },
] as const;

const COMPARISONS = [
  {
    them: 'Generic AI copilot',
    themDetail: 'One prompt over a CSV export',
    us: 'Patch Brief',
    usDetail: 'L0–L7 pipeline with grounded tools before the LLM writes',
  },
  {
    them: 'Invented win rates',
    themDetail: 'Model guesses stats from unstructured logs',
    us: 'Computed metrics',
    usDetail: 'L3 derives win rate and pick rate from telemetry you upload',
  },
  {
    them: 'Auto-patch commands',
    themDetail: '"Nerf by 11%" with no evidence trail',
    us: 'Designer decides',
    usDetail: 'Risks, solution paths, and validation — every claim cited',
  },
  {
    them: 'Community ignored',
    themDetail: 'Numbers alone, no perception layer',
    us: 'Alignment layer',
    usDetail: 'L5 flags when player voice and live data diverge',
  },
] as const;

const WHY_EXPLAIN = [
  {
    title: 'Grounded agent, not a chatbot',
    body: 'L0–L6 are tools and guardrails. L7 is the analyst that narrates — it cannot invent stats because evidence is structured first.',
  },
  {
    title: 'Reproducible for demos and reviews',
    body: 'Same JSON in, same patterns out. Judges and designers can trace every risk back to metrics, rules, or community clusters.',
  },
  {
    title: 'Built for pre-ship decisions',
    body: 'Not an auto-patcher. Output is a brief with cohorts, alignment, risks, and validation steps your team can act on.',
  },
] as const;

interface LandingPageProps {
  onNavigate: (path: AppRoute) => void;
}

export function LandingPage({ onNavigate }: LandingPageProps) {
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    rootRef.current?.scrollTo({ top: 0, behavior: 'auto' });
  }, []);

  useGSAP(
    () => {
      const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      const scroller = rootRef.current;
      if (!scroller) return;

      if (!reduced) {
        ScrollTrigger.defaults({ scroller });
      }

      if (reduced) return;

      gsap.from('[data-hero]', {
        y: 24,
        opacity: 0,
        duration: 0.55,
        stagger: 0.08,
        ease: 'power2.out',
      });

      gsap.utils.toArray<HTMLElement>('[data-reveal]').forEach((el) => {
        gsap.from(el, {
          scrollTrigger: { trigger: el, scroller, start: 'top 85%', once: true },
          y: 16,
          opacity: 0,
          duration: 0.45,
          ease: 'power2.out',
        });
      });

      return () => {
        ScrollTrigger.defaults({ scroller: undefined });
      };
    },
    { scope: rootRef },
  );

  return (
    <div className="landing landing-snap" ref={rootRef}>
      <div className="landing-grid-layer" aria-hidden="true">
        <GridParticles containerRef={rootRef} />
      </div>
      <div className="landing-content">
      {/* 1. Hero */}
      <section className="x-section x-section--dark x-section--snap x-section--hero" aria-labelledby="hero-title">
        <div className="x-section-fx" aria-hidden="true" />
        <div className="x-section-body">
          <div className="x-container hero-grid">
          <div>
            <p className="x-kicker" data-hero>
              Balance intelligence
            </p>
            <h1 id="hero-title" data-hero>
              Frame the patch
              <br />
              <em>before it ships</em>
            </h1>
            <p className="x-lead" data-hero>
              Merge telemetry, playtests, design rules, update plans, and community signal into one
              evidence-backed decision brief.
            </p>
            <div className="hero-actions" data-hero>
              <Button onClick={() => onNavigate('/analyze')} icon="pulse" iconAfter="arrow-right">
                Start analysis
              </Button>
              <Button variant="secondary" onClick={() => onNavigate('/report')}>
                View sample report
              </Button>
            </div>
            <ul className="hero-trust" data-hero>
              <li>
                <Icon name="check" size={14} />
                Evidence-led
              </li>
              <li>
                <Icon name="check" size={14} />
                L5/L6 guardrails
              </li>
              <li>
                <Icon name="check" size={14} />
                Designer decides
              </li>
            </ul>
          </div>
          <div className="hero-stats" data-hero aria-hidden="true">
            <div className="hero-stat">
              <span>Data vs community</span>
              <strong className="warn">Divergent</strong>
            </div>
            <div className="hero-stat">
              <span>L5 pattern</span>
              <strong>perception vs data</strong>
            </div>
            <div className="hero-stat">
              <span>Primary risk</span>
              <strong className="warn">stakeholder conflict</strong>
            </div>
          </div>
          </div>
        </div>
      </section>

      {/* 2. Big video */}
      <section className="x-section x-section--snap x-section--video" aria-labelledby="video-title">
        <div className="x-section-fx" aria-hidden="true" />
        <div className="x-section-body">
          <div className="x-container">
          <header className="x-section-head" data-reveal>
            <h2 id="video-title">See it run</h2>
            <p className="x-lead">
              From structured telemetry upload to a full decision brief — tools first, AI last.
            </p>
          </header>
          <div data-reveal>
            <LandingVideo />
          </div>
          </div>
        </div>
      </section>

      {/* 3. What is this product */}
      <section className="x-section x-section--cream x-section--snap x-section--about" aria-labelledby="about-title">
        <div className="x-section-fx" aria-hidden="true" />
        <div className="x-section-body">
          <div className="x-container">
          <header className="x-section-head" data-reveal>
            <h2 id="about-title">What is Patch Brief</h2>
            <p className="x-lead">
              A pre-ship balance copilot that turns studio JSON into an inspectable decision brief —
              not an automatic patch.
            </p>
          </header>
          <div className="about-grid" data-reveal>
            {ABOUT_POINTS.map((item) => (
              <article className="about-card" key={item.title}>
                <h3>{item.title}</h3>
                <p>{item.body}</p>
              </article>
            ))}
          </div>
          </div>
        </div>
      </section>

      {/* 4. Pipeline */}
      <section id="pipeline" className="x-section x-section--snap x-section--dense x-section--pipeline" aria-labelledby="pipeline-title">
        <div className="x-section-fx" aria-hidden="true" />
        <div className="x-section-body">
          <div className="x-container">
            <header className="x-section-head x-section-head--compact" data-reveal>
              <h2 id="pipeline-title">The L0–L7 pipeline</h2>
              <p className="x-lead">
                Deterministic tools through L6. The balance agent at L7 writes only from structured
                evidence. Tap a milestone to read that step.
              </p>
            </header>
          </div>
          <PipelineFlow scrollerRef={rootRef} />
        </div>
      </section>

      {/* 5. Why use us */}
      <section className="x-section x-section--gray x-section--snap x-section--dense x-section--why" aria-labelledby="why-title">
        <div className="x-section-fx" aria-hidden="true" />
        <div className="x-section-body">
          <div className="x-container">
          <header className="x-section-head" data-reveal>
            <h2 id="why-title">Why Patch Brief</h2>
            <p className="x-lead">
              Hackathon demos often wrap a CSV in ChatGPT. Studios need evidence, rules, and player
              perception in one place.
            </p>
          </header>
          <div className="why-grid">
            <div className="compare-grid" data-reveal role="table" aria-label="Comparison table">
              <div className="compare-head compare-row" role="row">
                <span role="columnheader">Typical AI</span>
                <span role="columnheader">Patch Brief</span>
              </div>
              {COMPARISONS.map((row) => (
                <div className="compare-row" role="row" key={row.them}>
                  <div className="compare-cell" role="cell">
                    <strong>{row.them}</strong>
                    <p>{row.themDetail}</p>
                  </div>
                  <div className="compare-cell compare-us" role="cell">
                    <strong>{row.us}</strong>
                    <p>{row.usDetail}</p>
                  </div>
                </div>
              ))}
            </div>
            <div data-reveal>
              <p className="x-body">
                Patch Brief is a grounded agentic copilot: C# tools compute and validate, guardrails
                attach evidence, and the LLM narrates the brief your designers can trust.
              </p>
              <ul className="explain-list">
                {WHY_EXPLAIN.map((item) => (
                  <li key={item.title}>
                    <strong>{item.title}</strong>
                    {item.body}
                  </li>
                ))}
              </ul>
            </div>
          </div>
          </div>
        </div>
      </section>

      {/* 6. Try now */}
      <section className="x-section x-section--dark x-section--snap x-section--cta" aria-labelledby="try-title">
        <div className="x-section-fx" aria-hidden="true" />
        <div className="x-section-body">
          <div className="x-container">
          <div className="try-panel" data-reveal>
            <h2 id="try-title">Try it now</h2>
            <p>Load the demo case or upload your studio JSON. The designer decides what ships.</p>
            <div className="try-actions">
              <Button onClick={() => onNavigate('/analyze')} icon="pulse" iconAfter="arrow-right">
                Open analysis workspace
              </Button>
              <Button variant="secondary" onClick={() => onNavigate('/report')}>
                View sample report
              </Button>
            </div>
          </div>
          </div>
        </div>
      </section>
      </div>
    </div>
  );
}
