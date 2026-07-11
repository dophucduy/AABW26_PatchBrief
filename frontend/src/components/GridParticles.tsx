import { useEffect, useRef, type RefObject } from 'react';

interface GridParticlesProps {
  containerRef: RefObject<HTMLElement | null>;
}

const CELL = 48;
const PARTICLE_RADIUS = 1.6;
const GLOW_RADIUS = 160;
const MAX_GLOW = 0.45;

export function GridParticles({ containerRef }: GridParticlesProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const mouseRef = useRef({ x: -9999, y: -9999 });
  const frameRef = useRef(0);

  useEffect(() => {
    const container = containerRef.current;
    const canvas = canvasRef.current;
    if (!container || !canvas) return;

    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let width = 0;
    let height = 0;
    let dpr = 1;

    const measure = () => {
      const rect = container.getBoundingClientRect();
      width = Math.max(1, Math.floor(rect.width));
      height = Math.max(1, Math.floor(rect.height));
    };

    const resize = () => {
      measure();
      dpr = Math.min(window.devicePixelRatio || 1, 2);
      canvas.width = Math.floor(width * dpr);
      canvas.height = Math.floor(height * dpr);
      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const onMove = (event: MouseEvent) => {
      const rect = container.getBoundingClientRect();
      mouseRef.current = {
        x: event.clientX - rect.left,
        y: event.clientY - rect.top,
      };
    };

    const onLeave = () => {
      mouseRef.current = { x: -9999, y: -9999 };
    };

    const draw = (time: number) => {
      if (width < 2 || height < 2) {
        frameRef.current = requestAnimationFrame(draw);
        return;
      }

      ctx.clearRect(0, 0, width, height);

      const mouse = mouseRef.current;
      const cols = Math.ceil(width / CELL) + 1;
      const rows = Math.ceil(height / CELL) + 1;

      for (let row = 0; row < rows; row += 1) {
        for (let col = 0; col < cols; col += 1) {
          const x = col * CELL;
          const y = row * CELL;

          const dx = x - mouse.x;
          const dy = y - mouse.y;
          const dist = Math.hypot(dx, dy);
          const hoverBoost = dist < GLOW_RADIUS ? (1 - dist / GLOW_RADIUS) * MAX_GLOW : 0;
          const breathe = reduced ? 0.03 : Math.sin(time * 0.001 + col * 0.35 + row * 0.28) * 0.06;
          const alpha = 0.22 + breathe + hoverBoost;

          ctx.beginPath();
          ctx.fillStyle = `rgba(255, 97, 1, ${alpha})`;
          ctx.arc(x, y, PARTICLE_RADIUS + hoverBoost * 5, 0, Math.PI * 2);
          ctx.fill();

          if (col < cols - 1) {
            ctx.strokeStyle = `rgba(255, 97, 1, ${0.12 + hoverBoost * 0.25})`;
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.moveTo(x, y);
            ctx.lineTo(x + CELL, y);
            ctx.stroke();
          }

          if (row < rows - 1) {
            ctx.strokeStyle = `rgba(255, 97, 1, ${0.12 + hoverBoost * 0.25})`;
            ctx.beginPath();
            ctx.moveTo(x, y);
            ctx.lineTo(x, y + CELL);
            ctx.stroke();
          }
        }
      }

      if (!reduced && mouse.x > -1000) {
        const grad = ctx.createRadialGradient(mouse.x, mouse.y, 0, mouse.x, mouse.y, GLOW_RADIUS);
        grad.addColorStop(0, 'rgba(255, 97, 1, 0.14)');
        grad.addColorStop(1, 'rgba(255, 97, 1, 0)');
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, width, height);
      }

      frameRef.current = requestAnimationFrame(draw);
    };

    resize();
    frameRef.current = requestAnimationFrame(draw);

    const ro = new ResizeObserver(resize);
    ro.observe(container);
    window.addEventListener('mousemove', onMove);
    container.addEventListener('mouseleave', onLeave);
    window.addEventListener('resize', resize);

    const boot = requestAnimationFrame(() => {
      resize();
    });

    return () => {
      cancelAnimationFrame(frameRef.current);
      cancelAnimationFrame(boot);
      ro.disconnect();
      window.removeEventListener('mousemove', onMove);
      container.removeEventListener('mouseleave', onLeave);
      window.removeEventListener('resize', resize);
    };
  }, [containerRef]);

  return <canvas className="landing-grid-canvas" ref={canvasRef} aria-hidden="true" />;
}
