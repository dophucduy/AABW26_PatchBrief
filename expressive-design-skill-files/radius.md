# Border Radius

| Token | Value | Default usage |
|---|---|---|
| base | 0px | Cards, modals, sections, tables, accordions, containers |
| default | 999px | Buttons, inputs, badges, tooltips, dropdown items, small controls |
| sm | 2px | Checkboxes, tiny elements |
| full | 9999px | Avatars, toggles, dot indicators |

## Rules

- 0px (sharp corners) is the default for all container-type components (cards, sections, modals, tables)
- 999px (pill shape) is the default for all interactive small components (buttons, inputs, badges)
- Never use arbitrary radius values outside this scale
- Radius must be consistent within each component family
