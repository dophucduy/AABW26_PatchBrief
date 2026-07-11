# Shadows

| Token | CSS value |
|---|---|
| shadow-2xs | `0 1px rgb(0 0 0 / 0.02)` |
| shadow-xs | `0 1px 2px 0 rgb(0 0 0 / 0.03)` |
| shadow-sm | `0 1px 3px 0 rgb(0 0 0 / 0.04), 0 1px 2px -1px rgb(0 0 0 / 0.03)` |
| shadow-md | `0 4px 6px -1px rgb(0 0 0 / 0.05), 0 2px 4px -2px rgb(0 0 0 / 0.03)` |
| shadow-lg | `0 10px 15px -3px rgb(0 0 0 / 0.05), 0 4px 6px -4px rgb(0 0 0 / 0.03)` |
| shadow-xl | `0 20px 25px -5px rgb(0 0 0 / 0.06), 0 8px 10px -6px rgb(0 0 0 / 0.04)` |
| shadow-2xl | `0 25px 50px -12px rgb(0 0 0 / 0.12)` |

## Component Mapping

| Component type | Token |
|---|---|
| Subtle separators, tiny UI details | shadow-2xs or shadow-xs |
| Inputs, buttons, small controls, lightweight cards | shadow-xs or shadow-sm |
| Standard cards, popovers, dropdowns | shadow-sm |
| Prominent cards, sticky surfaces | shadow-md |
| Modals, high-priority overlays | shadow-lg |
| Hero overlays, top-level emphasis (sparingly) | shadow-xl |

## Rules

- Use only these tokens — no custom box-shadow values
- Prefer borders over shadows for separation; shadows are secondary
- Keep elevation steps intentional; avoid jumping multiple levels
- Components in the same family share the same baseline elevation
- Hover/focus on interactive elevated elements: step up by one level
- Never stack multiple shadow tokens on one element
- Never use shadow-xl/shadow-2xl for dense list items or body containers
