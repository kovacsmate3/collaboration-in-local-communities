import { cn } from "@/lib/utils"

interface RichTextContentProps {
  html: string
  className?: string
}

/**
 * Renders user-authored HTML coming from the rich-text editor / markdown
 * pipeline. Single source of truth for typography across all rendered HTML
 * surfaces (task descriptions, terms page, admin terms preview) so the
 * three sites stay visually identical for the same input — see issue #244.
 *
 * The styling rides on Tailwind Typography's `prose` family of classes,
 * registered via the `@plugin "@tailwindcss/typography"` directive in
 * `app/globals.css`. The `prose-sm` size variant matches the rest of the
 * app's `text-sm` body copy; `dark:prose-invert` flips the palette in the
 * dark theme. `max-w-none` opts out of the plugin's default 65ch reading
 * width so the host container's width wins.
 *
 * Pass extra Tailwind classes via `className` to layer site-specific
 * tweaks (e.g. additional bottom margin) without forking the component.
 */
export function RichTextContent({ html, className }: RichTextContentProps) {
  return (
    <div
      className={cn(
        "prose prose-sm max-w-none prose-neutral dark:prose-invert",
        className
      )}
      dangerouslySetInnerHTML={{ __html: html }}
    />
  )
}
