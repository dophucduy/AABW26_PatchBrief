import { useEffect, useState } from 'react';
import { Icon } from './Ui';

function useReducedMotion() {
  const [reduced, setReduced] = useState(false);
  useEffect(() => {
    const media = window.matchMedia('(prefers-reduced-motion: reduce)');
    const update = () => setReduced(media.matches);
    update();
    media.addEventListener('change', update);
    return () => media.removeEventListener('change', update);
  }, []);
  return reduced;
}

function useCountUp(target: number, duration = 1350) {
  const reduceMotion = useReducedMotion();
  const [value, setValue] = useState(reduceMotion ? target : 0);
  useEffect(() => {
    if (reduceMotion) {
      setValue(target);
      return;
    }
    let frame = 0;
    const start = performance.now();
    const tick = (now: number) => {
      const progress = Math.min((now - start) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValue(Math.round(target * eased));
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [duration, reduceMotion, target]);
  return value;
}

export function TelemetryVisual({ impact = 38 }: { impact?: number }) {
  const count = useCountUp(impact);
  return <section className="telemetry" aria-label={`${impact}% low-bracket impact signal`}>
    <div className="telemetry-grid"></div>
    <div className="telemetry-orbit orbit-a"></div>
    <div className="telemetry-orbit orbit-b"></div>
    <div className="telemetry-orbit orbit-c"></div>
    <div className="telemetry-sweep"></div>
    <div className="telemetry-core">
      <span className="telemetry-kicker">LIVE SIGNAL</span>
      <strong>{count}<small>%</small></strong>
      <span className="telemetry-caption">low-bracket impact</span>
    </div>
    <div className="telemetry-tag tag-risk"><span><i></i>Risk index</span><b>high</b></div>
    <div className="telemetry-tag tag-aligned"><span><i></i>Playtest / live</span><b>aligned</b></div>
    <div className="telemetry-tag tag-divergent"><span><i></i>Community / data</span><b>divergent</b></div>
    <div className="telemetry-node node-one"><Icon name="pulse" size={15} /></div>
    <div className="telemetry-node node-two"><Icon name="scan" size={15} /></div>
  </section>;
}
