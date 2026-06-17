import Link from "next/link"
import { getTranslations } from "next-intl/server"

import { Button } from "@/components/ui/button"

/**
 * Global 404 page rendered by Next.js for any route that doesn't match a
 * page file (including the case where `notFound()` is called from a server
 * component and there's no nearer `not-found.tsx`). Copy is localized via
 * the `notFound` namespace so the page reads naturally in whichever locale
 * the `NEXT_LOCALE` cookie selected.
 */
export default async function NotFound() {
  const t = await getTranslations("notFound")

  return (
    <main className="flex min-h-svh items-center justify-center px-6">
      <div className="flex max-w-md flex-col items-center gap-4 text-center">
        <p className="font-mono text-caption tracking-[0.25em] text-muted-foreground uppercase">
          404
        </p>
        <h1 className="font-heading text-page-title">{t("heading")}</h1>
        <p className="text-body-sm text-muted-foreground">{t("body")}</p>
        <Button asChild>
          <Link href="/feed">{t("backToFeed")}</Link>
        </Button>
      </div>
    </main>
  )
}
