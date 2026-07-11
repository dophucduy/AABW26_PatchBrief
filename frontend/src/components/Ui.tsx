import { forwardRef } from 'react';
import type { ButtonHTMLAttributes, ChangeEvent, ReactNode } from 'react';

export type IconName =
  | 'arrow-left' | 'arrow-right' | 'check' | 'chevron-down' | 'close' | 'copy'
  | 'database' | 'file' | 'layers' | 'plus' | 'pulse' | 'scan' | 'spark' | 'upload' | 'warning'
  | 'shield' | 'chart' | 'brain' | 'book';

interface IconProps {
  name: IconName;
  size?: number;
  strokeWidth?: number;
}

export function Icon({ name, size = 18, strokeWidth = 1.8 }: IconProps) {
  const paths: Record<IconName, ReactNode> = {
    'arrow-left': <><path d="M20 12H4" /><path d="m10 18-6-6 6-6" /></>,
    'arrow-right': <><path d="M4 12h16" /><path d="m14 6 6 6-6 6" /></>,
    check: <path d="m5 12 4 4L19 6" />,
    'chevron-down': <path d="m7 10 5 5 5-5" />,
    close: <><path d="m6 6 12 12" /><path d="m18 6-12 12" /></>,
    copy: <><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" /></>,
    database: <><ellipse cx="12" cy="5" rx="8" ry="3" /><path d="M4 5v14c0 1.7 3.6 3 8 3s8-1.3 8-3V5" /><path d="M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3" /></>,
    file: <><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z" /><path d="M14 2v6h6" /><path d="M8 13h8" /><path d="M8 17h5" /></>,
    layers: <><path d="m12 3 9 5-9 5-9-5 9-5Z" /><path d="m3 12 9 5 9-5" /><path d="m3 16 9 5 9-5" /></>,
    plus: <><path d="M12 5v14" /><path d="M5 12h14" /></>,
    pulse: <path d="M3 12h3l2-7 4 14 2-7h7" />,
    scan: <><path d="M3 7V5a2 2 0 0 1 2-2h2" /><path d="M17 3h2a2 2 0 0 1 2 2v2" /><path d="M21 17v2a2 2 0 0 1-2 2h-2" /><path d="M7 21H5a2 2 0 0 1-2-2v-2" /><path d="M7 12h10" /></>,
    spark: <><path d="m12 3 1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8L12 3Z" /><path d="m19 16 .7 2.3L22 19l-2.3.7L19 22l-.7-2.3L16 19l2.3-.7L19 16Z" /></>,
    upload: <><path d="M12 16V4" /><path d="m7 9 5-5 5 5" /><path d="M5 20h14" /></>,
    warning: <><path d="m10.3 3.8-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.7-3.2l-8-14a2 2 0 0 0-3.4 0Z" /><path d="M12 9v4" /><path d="M12 17h.01" /></>,
    shield: <><path d="M12 3 4 7v6c0 5 3.5 8 8 8s8-3 8-8V7l-8-4Z" /><path d="m9 12 2 2 4-4" /></>,
    chart: <><path d="M4 20V10" /><path d="M10 20V4" /><path d="M16 20v-8" /><path d="M22 20V8" /></>,
    brain: <><path d="M12 4.5a2.5 2.5 0 0 0-4.96 1.02 2.5 2.5 0 0 0-1.51 4.23A3 3 0 0 0 5 15.5c0 2.5 2 4.5 4.5 4.5h5c2.5 0 4.5-2 4.5-4.5a3 3 0 0 0-2.53-2.75 2.5 2.5 0 0 0-1.51-4.23A2.5 2.5 0 0 0 12 4.5Z" /><path d="M12 9v6" /><path d="M9.5 12h5" /></>,
    book: <><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" /><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2Z" /><path d="M8 7h8" /><path d="M8 11h6" /></>,
  };
  return <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>;
}

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'outline';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  icon?: IconName;
  iconAfter?: IconName;
  children: ReactNode;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button({ variant = 'primary', icon, iconAfter, children, className = '', type = 'button', ...props }, ref) {
  return <button {...props} ref={ref} type={type} className={`button button-${variant} ${className}`.trim()}>
    {icon && <Icon name={icon} size={16} />}
    <span>{children}</span>
    {iconAfter && <Icon name={iconAfter} size={16} />}
  </button>;
});

export function Eyebrow({ children }: { children: ReactNode }) {
  return <p className="eyebrow"><span></span>{children}</p>;
}

interface AppShellProps {
  children: ReactNode;
  currentPath: string;
  apiOnline: boolean;
  onNavigate: (path: string) => void;
}

export function AppShell({ children, currentPath, apiOnline, onNavigate }: AppShellProps) {
  const isAnalyze = currentPath.startsWith('/analyze') || currentPath.startsWith('/mapping');
  const isLanding = currentPath === '/';
  return <div className={`app-shell${isLanding ? ' app-shell--landing' : ''}`}>
    <header className="topbar">
      <button className="brand" onClick={() => onNavigate('/')} aria-label="Go to Patch Brief home">
        <span className="brand-mark"><i></i><i></i><i></i></span>
        <span>PATCH<span>/</span>BRIEF</span>
      </button>
      <nav className="main-nav" aria-label="Primary navigation">
        <button className={isAnalyze ? 'active' : ''} onClick={() => onNavigate('/analyze')}>Analyze</button>
        <button className={currentPath === '/report' ? 'active' : ''} onClick={() => onNavigate('/report')}>Latest report</button>
      </nav>
      <div className={`connection-status ${apiOnline ? 'online' : ''}`}><i></i>{apiOnline ? 'API connected' : 'Demo workspace'}</div>
    </header>
    <main>{children}</main>
    <footer className="site-footer"><span>Patch Brief - balance intelligence for live games</span><span>Designer decides</span></footer>
  </div>;
}

interface BackLinkProps {
  label: string;
  onClick: () => void;
}

export function BackLink({ label, onClick }: BackLinkProps) {
  return <button className="back-link" onClick={onClick}><Icon name="arrow-left" size={15} />{label}</button>;
}

interface FileSlotProps {
  id: string;
  label: string;
  description: string;
  optional?: boolean;
  file?: File;
  onSelect: (file: File) => void;
}

export function FileSlot({ id, label, description, optional = false, file, onSelect }: FileSlotProps) {
  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const nextFile = event.target.files?.[0];
    if (nextFile) onSelect(nextFile);
  };
  return <label className={`file-slot ${file ? 'ready' : ''}`} htmlFor={id}>
    <input id={id} type="file" accept=".json,application/json" onChange={handleChange} />
    <span className="file-slot-icon"><Icon name={file ? 'check' : 'upload'} size={18} /></span>
    <span className="file-slot-copy"><strong>{label}</strong><small>{file ? `${(file.size / 1024).toFixed(1)} KB ready` : description}</small></span>
    <span className="file-slot-state">{file ? 'Ready' : optional ? 'Optional' : 'Required'}</span>
    <Icon name="chevron-down" size={16} />
  </label>;
}

export function RiskPill({ level }: { level: 'high' | 'medium' | 'low' }) {
  return <span className={`risk-pill ${level}`}><i></i>{level} risk</span>;
}
