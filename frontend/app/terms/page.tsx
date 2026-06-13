import type { Metadata } from "next"
import { Suspense } from "react"
import { getFormatter, getTranslations } from "next-intl/server"

import {
  TermsAcceptancePanel,
  TermsBackLink,
} from "@/components/legal/terms-acceptance-panel"
import { RichTextContent } from "@/components/shared/rich-text-content"
import {
  DEFAULT_BACKEND_API_URL,
  BACKEND_TERMS_PATHS,
} from "@/lib/auth/constants"

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("terms")
  return {
    title: t("pageTitle"),
  }
}

interface ActiveTermsResponse {
  id: string
  version: string
  title: string
  content: string | null
  contentUrl: string | null
  effectiveFrom: string
}

async function fetchActiveTerms(): Promise<ActiveTermsResponse | null> {
  try {
    const baseUrl = process.env.API_URL ?? DEFAULT_BACKEND_API_URL
    const url = new URL(`/api/${BACKEND_TERMS_PATHS.active.join("/")}`, baseUrl)
    const res = await fetch(url.toString(), { cache: "no-store" })
    if (!res.ok) return null
    return (await res.json()) as ActiveTermsResponse
  } catch {
    return null
  }
}

export default async function TermsPage() {
  const activeTerms = await fetchActiveTerms()
  const t = await getTranslations("terms")
  const format = await getFormatter()

  return (
    <div className="mx-auto max-w-2xl px-6 py-16">
      <Suspense fallback={null}>
        <TermsBackLink />
        <TermsAcceptancePanel />
      </Suspense>

      <h1 className="mb-2 text-3xl font-semibold tracking-tight">
        {t("heading")}
      </h1>
      {activeTerms && (
        <p className="mb-10 text-sm text-muted-foreground">
          {t("versionLine", {
            version: activeTerms.version,
            date: format.dateTime(new Date(activeTerms.effectiveFrom), {
              dateStyle: "medium",
            }),
          })}
        </p>
      )}

      {activeTerms?.content ? (
        <RichTextContent html={activeTerms.content} />
      ) : (
        <StaticPlaceholderContent />
      )}
    </div>
  )
}

async function StaticPlaceholderContent() {
  const t = await getTranslations("terms.sections")
  const sections = [
    "about",
    "account",
    "acceptableUse",
    "payments",
    "privacy",
    "liability",
    "changes",
  ] as const

  return (
    <div className="prose flex flex-col gap-8 text-sm leading-relaxed prose-neutral dark:prose-invert">
      {sections.map((id) => (
        <section key={id} className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">{t(`${id}.title`)}</h2>
          <p className="text-muted-foreground">{t(`${id}.body`)}</p>
        </section>
      ))}
    </div>
  )
}
