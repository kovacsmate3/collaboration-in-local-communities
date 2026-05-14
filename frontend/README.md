# Frontend

Next.js 16 app for `2gather`, built with React 19, Tailwind v4, and shadcn/ui.

## Design System

Design tokens live in `app/globals.css` and are documented in
[`docs/design-tokens.md`](./docs/design-tokens.md). The visual component gallery
is available at `/component-gallery` during local development and returns 404 in
production.

## Adding components

To add components to your app, run the following command:

```bash
npx shadcn@latest add button
```

This will place the ui components in the `components` directory.

## Using components

To use the components in your app, import them as follows:

```tsx
import { Button } from "@/components/ui/button"
```
