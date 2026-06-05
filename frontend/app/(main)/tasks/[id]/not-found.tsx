import Link from "next/link"
import { getTranslations } from "next-intl/server"

import { Button } from "@/components/ui/button"

export default async function TaskNotFound() {
  const t = await getTranslations("tasks.notFound")

  return (
    <main className="flex flex-1 items-center justify-center px-6 py-24">
      <div className="flex max-w-md flex-col items-center gap-4 text-center">
        <p className="font-mono text-xs tracking-[0.25em] text-muted-foreground uppercase">
          404
        </p>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("heading")}
        </h1>
        <p className="text-sm text-muted-foreground">{t("body")}</p>
        <Button asChild>
          <Link href="/tasks">{t("back")}</Link>
        </Button>
      </div>
    </main>
  )
}
