# Color Tokens


## Background Tokens

### Neutral
| Token | Light | Dark |
|---|---|---|
| neutral-primary-soft | #FFFFFF | #131416 |
| neutral-primary | #FFFFFF | #0A0B0D |
| neutral-primary-medium | #F9F6F0 | #1A1C1E |
| neutral-primary-strong | #F4F0E8 | #2A2C2E |
| neutral-secondary-soft | #F9F6F0 | #131416 |
| neutral-secondary | #F9F6F0 | #0A0B0D |
| neutral-secondary-medium | #F4F0E8 | #1A1C1E |
| neutral-secondary-strong | #EBE7DF | #2A2C2E |
| neutral-tertiary-soft | #E3E4DD | #1A1C1E |
| neutral-tertiary | #E3E4DD | #232628 |
| neutral-tertiary-medium | #D6D7D0 | #2E3032 |
| neutral-quaternary | #C8C9C2 | #2E3032 |
| quaternary-medium | #B8B9B3 | #3A3C3E |
| gray | #A0A19B | #4A4C4E |

### Brand
| Token | Light | Dark |
|---|---|---|
| brand-softer | #FFF5EC | #3D1800 |
| brand-soft | #FFD9B3 | #662800 |
| brand | #FF6101 | #FF7A2E |
| brand-medium | #FFB380 | #662800 |
| brand-strong | #CC4E01 | #FF6101 |

### Status
| Token | Light | Dark |
|---|---|---|
| success-soft | #ECFDF5 | #002C22 |
| success | #007A55 | #009966 |
| success-medium | #D0FAE5 | #004F3B |
| success-strong | #006045 | #007A55 |
| danger-soft | #FEF0F2 | #4D0218 |
| danger | #C70036 | #C70036 |
| danger-medium | #FFE4E6 | #8B0836 |
| danger-strong | #A50036 | #A50036 |
| warning-soft | #FFF7ED | #7C2D12 |
| warning | #F97316 | #F97316 |
| warning-medium | #FFEDD5 | #7C2D12 |
| warning-strong | #C2410C | #C2410C |

### Button Glint (CSS custom properties, used for the glint box-shadow effect)
| Variable | Light | Dark |
|---|---|---|
| `--color-1-400` | rgba(255,255,255,0.20) | rgba(255,255,255,0.10) |
| `--color-1-700` | rgba(0,0,0,0.08) | rgba(0,0,0,0.20) |

### Utility
| Token | Light | Dark |
|---|---|---|
| dark | #232628 | #232628 |
| dark-strong | #1A1C1E | #2E3032 |
| disabled | #E3E4DD | #232628 |

### Accent
| Token | Value (same both modes) |
|---|---|
| purple | #A855F7 |
| sky | #0EA5E9 |
| teal | #0D9488 |
| pink | #DB2777 |
| cyan | #06B6D4 |
| fuchsia | #C026D3 |
| indigo | #4F46E5 |
| orange | #FF6101 |

## Text Color Tokens

### Base
| Token | Light | Dark |
|---|---|---|
| white | #FFFFFF | #FFFFFF |
| black | #1A1C1E | #1A1C1E |
| heading | #1A1C1E | #F9F6F0 |
| body | #5A5C5E | #A0A19B |
| body-subtle | #7A7C7E | #8A8C8E |

### Brand
| Token | Light | Dark |
|---|---|---|
| fg-brand-subtle | #FFB380 | #662800 |
| fg-brand | #FF6101 | #FF7A2E |
| fg-brand-strong | #CC4E01 | #FFB380 |

### Status
| Token | Light | Dark |
|---|---|---|
| fg-success | #047857 | #065F46 |
| fg-success-strong | #065F46 | #10B981 |
| fg-danger | #BE123C | #F43F5E |
| fg-danger-strong | #881337 | #F87171 |
| fg-warning-subtle | #EA580C | #F97316 |
| fg-warning | #7C2D12 | #FBBF24 |
| fg-disabled | #A0A19B | #5A5C5E |

### Informational / Accent
| Token | Light | Dark |
|---|---|---|
| fg-yellow | #FACC15 | #FACC15 |
| fg-info | #312E81 | #93C5FD |
| fg-purple | #9333EA | #A855F7 |
| fg-purple-strong | #7E3AF2 | #DDD6FE |
| fg-cyan | #0891B2 | #06B6D4 |
| fg-indigo | #4F46E5 | #4F46E5 |
| fg-pink | #DB2777 | #DB2777 |
| fg-lime | #65A30D | #84CC16 |

## Border Color Tokens

| Token | Light | Dark |
|---|---|---|
| border-dark | #232628 | #4A4C4E |
| border-buffer | #FFFFFF | #0A0B0D |
| border-buffer-medium | #FFFFFF | #1A1C1E |
| border-buffer-strong | #FFFFFF | #2E3032 |
| border-muted | #F9F6F0 | #131416 |
| border-light-subtle | #E3E4DD | #131416 |
| border-light | #E3E4DD | #1A1C1E |
| border-light-medium | #D6D7D0 | #2E3032 |
| border-default-subtle | #D6D7D0 | #131416 |
| border-default | #C8C9C2 | #232628 |
| border-default-medium | #B8B9B3 | #2E3032 |
| border-default-strong | #A0A19B | #3A3C3E |
| border-success-subtle | #A7F3D0 | #064E3B |
| border-success | #047857 | #065F46 |
| border-danger-subtle | #FECDD3 | #881337 |
| border-danger | #BE123C | #BE123C |
| border-warning-subtle | #FED7AA | #7C2D12 |
| border-warning | #EA580C | #F97316 |
| border-brand-subtle | #FFD9B3 | #662800 |
| border-brand-light | #FF7A2E | #FF7A2E |
| border-brand | #FF6101 | #FF7A2E |
| border-dark-subtle | #232628 | #2E3032 |
| border-purple | #A855F7 | #A855F7 |
| border-orange | #FF6101 | #FF6101 |

## Semantic Usage Rules

- Page/section backgrounds: neutral-primary-soft (default white), neutral-secondary-soft (warm cream #F9F6F0), neutral-tertiary-soft (gray #E3E4DD for alternating)
- Dark sections (footer, hero overlays): dark (#232628)
- Primary buttons: brand background
- Headings: heading text color
- Body text: body text color
- CTA links: fg-brand text color
- Default borders: border-default
- Status borders match intent: success → border-success, danger → border-danger, warning → border-warning
- Disabled: disabled background + fg-disabled text

## Prohibited

- No raw hex/rgb values in component code — always use design tokens
- No brand text color for long-form paragraphs
- No accent text tokens (fg-purple, etc.) for body copy or navigation
- No brand/accent backgrounds for large layout surfaces (pages, sections) unless it's a hero/campaign area
- No manual light/dark value swapping — let the CSS custom properties handle it
