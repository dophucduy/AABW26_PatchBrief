# Layout & Spacing

## Spacing Rhythm

Base unit: **8px**. All spacing values should be multiples of 8px.

| Context | Value |
|---|---|
| Section vertical padding | 96px |
| Section header → content | 48px or 64px |
| Heading → paragraph | 16px |
| Container horizontal padding | 24px |
| Flex/grid row gap | 16px |
| Card grid gap | 24px |
| Wide component grid gap | 32px |
| Column layout gap | 48px |

## Container

Standard section container: max-width 1280px, centered, 24px horizontal padding.

Every major section wraps content in this container.

## Section Border Lines

Sections are separated by full-width horizontal border lines (1px solid, border-default color). These lines span the entire viewport width (edge to edge), while the content inside remains constrained to the 1280px container.

### Rules for Section Borders
- The top and bottom border of each section stretches 100% of the viewport width
- Content inside the section is centered in a max-width 1280px container with 24px horizontal padding
- Borders are straight and sharp — no rounded corners on section wrappers
- Use border-default color for the horizontal rules
- Avoid doubling borders between adjacent sections (one border between two sections, not two)

## Container Vertical Borders

The 1280px centered container has **left and right borders** (1px solid, border-default color) that run the full height of each section. This frames the content column and creates visible vertical rails down the page.

### Rules for Container Vertical Borders
- Apply `border-x` (left + right) with border-default color to the max-width 1280px container inside every section
- The vertical borders connect visually from section to section, forming continuous vertical lines down the page
- Combined with the full-width horizontal section borders, this creates a structured wireframe/grid aesthetic
- In dark sections (footer), use border-dark-subtle for the vertical borders instead of border-default

## Grid Border Lines (Component-Level)

Inside sections that contain multi-column grids (feature cards, testimonials, etc.), use 1px border lines **between every grid item** — both horizontally and vertically — to reinforce the structured grid aesthetic.

### Technique
- Set the grid container's background to the border color (`bg-border-default`) and use `gap-px` (1px gap)
- Each grid item gets its own background (`bg-neutral-primary-soft` or the appropriate section color)
- The 1px gap with the border-colored container background creates perfect 1px lines between all items without extra border declarations

### Rules for Grid Border Lines
- Use `gap-px` on the grid container, never manual borders on individual items
- The grid container background color must match the border color token (border-default)
- Grid items fill their cells with the section's background color
- This technique works for any grid layout: 2-column, 3-column, or mixed
- Section header areas above a grid should have a bottom border (`border-b border-border-default`) to separate the header from the grid items below

## Content Composition Order

Inside each section, follow this order:
1. Heading (`h1`–`h3`)
2. Leading paragraph
3. Normal paragraph(s)
4. Lists, CTA links, or component grids

## Section Pattern

Each section has:
- 96px vertical padding
- A full-viewport-width top border (1px solid, border-default)
- A background color (alternate between neutral-primary-soft, neutral-secondary-soft, and neutral-tertiary-soft)
- A centered container (max-width 1280px, 24px horizontal padding)
- A section header area with 48px bottom margin
- Section content below
- Dark sections (footer, hero overlays) use dark (#232628) background with white/light text

## Motion & Animation

- Prefer CSS-native: `transition`, `animation`, `@keyframes`. Use Motion library only when CSS cannot achieve the behavior.
- Prioritize high-impact orchestrated moments over scattered micro-interactions. A single well-sequenced page-load animation using staggered `animation-delay` delivers more perceived quality than many isolated effects.
- Reserve scroll-triggered and hover transitions for moments that reinforce hierarchy or reward attention.

## Backgrounds & Visual Depth

- Default to clean, flat backgrounds with strong border separation rather than layered atmospheric effects.
- Use solid fills (neutral-primary-soft, neutral-secondary-soft, neutral-tertiary-soft) with prominent 1px border lines between sections.
- Dark sections (#232628) for footer and high-contrast areas.
- Every decorative element must serve a compositional purpose (depth, separation, or emphasis). No purely ornamental effects competing with content.

## Must

- All sections: consistent 96px vertical padding
- All containers: max-width 1280px, centered, 24px horizontal padding
- All containers: left and right vertical borders (border-x, border-default) on the 1280px container
- All sections: full-width 1px top border (border-default) as separator
- All multi-column grids: gap-px with bg-border-default on grid container for 1px grid lines
- Section headers above grids: border-b border-default to separate header from grid
- Section headers: 48px or 64px bottom margin
- Consistent vertical rhythm, no crowded sections
- Layouts readable and properly spaced on both desktop and mobile
- Footer and dark sections: dark (#232628) background, border-dark-subtle for vertical borders
