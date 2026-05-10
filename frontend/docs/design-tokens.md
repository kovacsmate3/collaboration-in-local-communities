# Design Tokens

This is the reference for frontend design tokens in `2gather`.

The source of truth is split deliberately:

- `app/globals.css` defines CSS variables and exposes them as Tailwind v4
  utilities through `@theme inline`.
- `lib/design-tokens.ts` stores typed metadata used by the component gallery.
- `/component-gallery` renders the palette, typography, shape, elevation, and
  core component variants during local development. It returns 404 in
  production.

## Principles

- Prefer semantic utilities such as `bg-success`, `text-reputation`, or
  `bg-compensation-barter` over raw palette classes.
- Use shadcn/ui component variants before introducing one-off styling.
- Add a token only when a visual decision is reused or carries product meaning.
- Keep light and dark values paired in `:root` and `.dark`.
- Keep the gallery metadata in sync whenever a public token is added or removed.

## Color Tokens

| Group         | Tokens                                                                                                                                                            | Use                                                                |
| ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| Core surfaces | `background`, `foreground`, `card`, `card-foreground`, `popover`, `popover-foreground`, `border`, `input`, `ring`                                                 | App canvas, content surfaces, form borders, and focus rings.       |
| Actions       | `primary`, `primary-foreground`, `secondary`, `secondary-foreground`, `accent`, `accent-foreground`, `destructive`                                                | Buttons, active navigation, hover states, and destructive actions. |
| Feedback      | `success`, `success-foreground`, `warning`, `warning-foreground`, `info`, `info-foreground`, `reputation`, `reputation-foreground`                                | Task outcomes, alerts, help states, and trust/reputation cues.     |
| Domain        | `compensation-paid`, `compensation-credit`, `compensation-barter`                                                                                                 | Compensation modes that should stay named in UI code.              |
| Charts        | `chart-1` through `chart-5`                                                                                                                                       | Ordered dashboard chart series.                                    |
| Sidebar       | `sidebar`, `sidebar-foreground`, `sidebar-primary`, `sidebar-primary-foreground`, `sidebar-accent`, `sidebar-accent-foreground`, `sidebar-border`, `sidebar-ring` | App and admin navigation shells.                                   |

Tailwind examples:

```tsx
<Badge variant="success">Paid</Badge>
<div className="bg-compensation-barter text-warning-foreground" />
<HugeiconsIcon className="text-reputation" />
```

## Typography Tokens

| Token                  | Tailwind class       | Use                                                |
| ---------------------- | -------------------- | -------------------------------------------------- |
| `--font-heading`       | `font-heading`       | Page titles, section headings, and card titles.    |
| `--font-sans`          | `font-sans`          | Default application UI text.                       |
| `--font-mono`          | `font-mono`          | IDs, slugs, API paths, and audit values.           |
| `--text-display`       | `text-display`       | First-viewport product headlines.                  |
| `--text-page-title`    | `text-page-title`    | Main page titles inside the app shell.             |
| `--text-section-title` | `text-section-title` | Panel headings and form section titles.            |
| `--text-body`          | `text-body`          | Longer readable descriptions.                      |
| `--text-body-sm`       | `text-body-sm`       | Dense interface text in cards, tables, and forms.  |
| `--text-caption`       | `text-caption`       | Metadata, labels, helper text, and compact badges. |

Use typography tokens for repeated roles. For isolated layout adjustments, normal
Tailwind utilities such as `text-sm` or `font-medium` are still acceptable.

## Shape And Elevation Tokens

| Token             | Tailwind class  | Use                                        |
| ----------------- | --------------- | ------------------------------------------ |
| `--radius-sm`     | `rounded-sm`    | Small controls and inner elements.         |
| `--radius-md`     | `rounded-md`    | Buttons, inputs, and compact menu items.   |
| `--radius-lg`     | `rounded-lg`    | Tables, panels, and standard cards.        |
| `--radius-xl`     | `rounded-xl`    | Large cards and modal surfaces.            |
| `--radius-2xl`    | `rounded-2xl`   | Large media or spacious feature panels.    |
| `--radius-4xl`    | `rounded-4xl`   | Pills and compact badges.                  |
| `--shadow-card`   | `shadow-card`   | Subtle lift for repeated content surfaces. |
| `--shadow-raised` | `shadow-raised` | Floating overlays and elevated panels.     |

## Adding Or Changing Tokens

1. Add or update the variable in `app/globals.css`.
2. Expose it inside `@theme inline` if Tailwind should generate a utility.
3. Add matching metadata in `lib/design-tokens.ts`.
4. Render and inspect `/component-gallery`.
5. Update this document when the public token contract changes.

## When Not To Add A Token

Do not add a token for a single component tweak, temporary layout experiment, or
one-off illustration color. Keep those local until the value becomes a repeated
design decision.
