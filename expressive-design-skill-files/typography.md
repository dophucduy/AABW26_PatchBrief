# Typography

> Dependencies: `colors.md`

## Core Rules

- **Font:** "IBM Plex Mono", monospace — configured at app level, never override
- **Headings:** bold weight (700), heading text color, uppercase for h1–h2
- **Body copy:** body text color, never use brand color for paragraphs longer than one sentence
- **Semantic HTML:** Use `h1`–`h6` in order, never skip levels

## Heading Scale

### Desktop

| Element | Size | Line-height | Letter-spacing | Margin-bottom |
|---|---|---|---|---|
| `h1` | 72px | 1 | -1.5px | 32px |
| `h2` | 48px | 1.1 | -0.5px | — |
| `h3` | 36px | 1.15 | -0.3px | — |
| `h4` | 28px | 1.2 | — | — |
| `h5` | 22px | 1.4 | — | — |
| `h6` | 18px | 1.3 | — | — |

### Responsive

| Element | Tablet (≥768px) | Mobile (default) |
|---|---|---|
| `h1` | 48px | 36px |
| `h2` | 40px | 32px |
| `h3` | 30px | 24px |
| `h4` | 24px | 20px |
| `h5` | 20px | 18px |
| `h6` | 16px | 16px |

Mobile-first: start with mobile sizes, scale up at tablet and desktop breakpoints.

Never reduce line-height below 1.1 for any heading.

## Paragraphs

### Leading Paragraph
- Size: 18px
- Weight: normal
- Color: body
- Line-height: 1.7
- Max width: ~65 characters

### Normal Paragraph
- Size: 15px
- Weight: normal
- Color: body
- Line-height: 1.7
- Max width: ~60 characters

### Small Supporting Copy
- Size: 13px
- Weight: normal
- Color: body
- Line-height: 1.6
- Use only for helper text, legal text, captions, metadata.

## UI Labels

| Context | Size | Weight |
|---|---|---|
| Button labels | 15px | 500 (medium) |
| Input labels | 14px or 15px | 500 (medium) |
| Captions / meta / badges | 12px or 13px | 500 (medium) |

Do not apply paragraph line-height (1.7) to control labels.

## Links

- **Inline links:** Same size as surrounding text, fg-brand color, underline, hover → no underline
- **CTA links:** fg-brand color, medium weight, underline, hover → no underline

## Emphasis

- `<strong>` for high-priority emphasis in body text
- `<em>` for tone emphasis only, not visual hierarchy
- All-caps only for short labels: uppercase, 1px letter-spacing, 12px or 13px

## Dark Mode

Hierarchy stays identical. Only color tokens change (automatic via CSS custom properties). Size, weight, and spacing remain constant.
