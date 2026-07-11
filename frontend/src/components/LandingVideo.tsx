import { useRef, useState } from 'react';
import gsap from 'gsap';
import { useGSAP } from '@gsap/react';

const DEMO_LINES = [
  '> ingest telemetry_live.json … 5 rows',
  '> ingest telemetry_playtest.json … 1 row',
  '> L3 metrics: char_A bronze wr=0.58',
  '> L4 context: nerf base_damage 45→40',
  '> L5 pattern: perception_vs_data_divergence',
  '> L6 risk: stakeholder_conflict [high]',
  '> L7 report: executive_summary ready',
];

export function LandingVideo() {
  const shellRef = useRef<HTMLDivElement>(null);
  const [useFallback, setUseFallback] = useState(false);

  useGSAP(
    () => {
      const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
      if (reduced || !useFallback) return;

      const lines = shellRef.current?.querySelectorAll('.demo-line');
      if (!lines?.length) return;

      gsap.set(lines, { opacity: 0.35 });
      const tl = gsap.timeline({ repeat: -1, repeatDelay: 1.2 });
      lines.forEach((line, index) => {
        tl.to(line, { opacity: 1, duration: 0.2, ease: 'power2.out' }, index * 0.45);
        if (index > 2) {
          tl.to(line, { opacity: 0.5, duration: 0.15 }, index * 0.45 + 0.35);
        }
      });
    },
    { scope: shellRef, dependencies: [useFallback] },
  );

  return (
    <div className="video-stage">
      {!useFallback ? (
        <video
          className="video-player"
          controls
          playsInline
          preload="metadata"
          poster="/patch-brief-poster.svg"
          onError={() => setUseFallback(true)}
        >
          <source src="/patch-brief-demo.mp4" type="video/mp4" />
          <source src="/patch-brief-demo.webm" type="video/webm" />
        </video>
      ) : (
        <div className="video-demo-fallback" ref={shellRef} aria-label="Pipeline demo animation">
          <div className="demo-chrome">
            <span>PATCH_BRIEF / analyze</span>
            <b>LIVE DEMO</b>
          </div>
          <div className="demo-body">
            {DEMO_LINES.map((line) => (
              <p className="demo-line" key={line}>
                {line}
              </p>
            ))}
          </div>
        </div>
      )}
      <p className="video-caption">
        {useFallback
          ? 'Animated pipeline trace — drop patch-brief-demo.mp4 in /public to replace.'
          : 'Watch the L0–L7 flow from upload to decision brief.'}
      </p>
    </div>
  );
}
