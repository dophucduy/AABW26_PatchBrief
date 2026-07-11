# Badges

> Dependencies: `colors.md`, `radius.md`

## Core Specs

- **Border:** 1px
- **Default radius:** 999px — all badges are pill-shaped
- **Pill radius:** 999px

## Sizes

| Size | Font size | Horizontal padding | Vertical padding |
|---|---|---|---|
| Default (small) | 12px | 10px | 4px |
| Large | 14px | 12px | 6px |

## Variants

### Brand
- **Background:** brand-softer
- **Border:** border-brand-subtle
- **Text:** fg-brand-strong

### Alternative (Neutral Soft)
- **Background:** neutral-primary-soft
- **Border:** border-default
- **Text:** heading

### Gray (Neutral Medium)
- **Background:** neutral-secondary-medium
- **Border:** border-default
- **Text:** heading

### Danger
- **Background:** danger-soft
- **Border:** border-danger-subtle
- **Text:** fg-danger-strong

### Success
- **Background:** success-soft
- **Border:** border-success-subtle
- **Text:** fg-success-strong

### Warning
- **Background:** warning-soft
- **Border:** border-warning-subtle
- **Text:** fg-warning

### Dark
- **Background:** dark
- **Border:** transparent
- **Text:** white

## Pill Badges

All badges use 999px radius by default (pill shape).

## Badges with Icons

- Icon size (default): 12x12px
- Icon size (large): 14x14px
- Icon spacing: 4px margin next to label

## Icon-only Badge

Square shape — equalize dimensions to 24x24px, no horizontal text padding.

## Dismissible Badges

Badge content + a close button. Close button hover backgrounds per variant:

| Variant | Close button hover background |
|---|---|
| Brand | brand-soft |
| Alternative | neutral-tertiary |
| Gray | neutral-quaternary |
| Danger | danger-medium |
| Success | success-medium |
| Warning | warning-medium |

## Dot / Notification Badge

- Positioned absolutely: -4px top, -4px right
- Size: 12x12px, fully rounded
- 2px border in border-buffer color
- Background: danger
