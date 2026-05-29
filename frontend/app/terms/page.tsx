import type { Metadata } from "next"
import { Suspense } from "react"

import {
  TermsAcceptancePanel,
  TermsBackLink,
} from "@/components/legal/terms-acceptance-panel"
import {
  DEFAULT_BACKEND_API_URL,
  BACKEND_TERMS_PATHS,
} from "@/lib/auth/constants"

export const metadata: Metadata = {
  title: "Terms & Conditions",
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

  return (
    <div className="mx-auto max-w-2xl px-6 py-16">
      <Suspense fallback={null}>
        <TermsBackLink />
        <TermsAcceptancePanel />
      </Suspense>

      <h1 className="mb-2 text-3xl font-semibold tracking-tight">
        Terms &amp; Conditions
      </h1>
      {activeTerms && (
        <p className="mb-10 text-sm text-muted-foreground">
          Version {activeTerms.version} &mdash; effective{" "}
          {new Date(activeTerms.effectiveFrom).toLocaleDateString()}
        </p>
      )}

      {activeTerms?.content ? (
        <div
          className="prose prose-neutral dark:prose-invert flex flex-col gap-4 text-sm leading-relaxed"
          dangerouslySetInnerHTML={{ __html: activeTerms.content }}
        />
      ) : (
        <StaticPlaceholderContent />
      )}
    </div>
  )
}

function StaticPlaceholderContent() {
  return (
    <div className="prose prose-neutral dark:prose-invert flex flex-col gap-8 text-sm leading-relaxed">
      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">1. About 2gather</h2>
        <p className="text-muted-foreground">
          2gather is a platform that connects neighbours who need help with
          those willing to offer it. By creating an account you agree to use the
          service in good faith and in accordance with your local laws.
        </p>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">2. Your account</h2>
        <p className="text-muted-foreground">
          You are responsible for keeping your credentials secure and for all
          activity that occurs under your account. Notify us immediately if you
          suspect unauthorised access.
        </p>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">3. Acceptable use</h2>
        <p className="text-muted-foreground">
          You may not use 2gather to post illegal tasks, harass other users,
          misrepresent your identity, or scrape the platform without written
          permission. We reserve the right to suspend accounts that violate
          these rules.
        </p>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">4. Payments &amp; barter</h2>
        <p className="text-muted-foreground">
          2gather facilitates agreements between users but is not a party to
          them. Any payment or exchange is solely between the Seeker and the
          Helper. We are not liable for disputes arising from tasks.
        </p>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">5. Privacy</h2>
        <p className="text-muted-foreground">
          We collect only the information needed to operate the service. We do
          not sell your data. A full privacy policy will be published before the
          public launch.
        </p>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">6. Limitation of liability</h2>
        <p className="text-muted-foreground">
          2gather is provided &ldquo;as is&rdquo; during this early phase. We
          make no warranties about uptime or fitness for a particular purpose.
          Our liability is limited to the maximum extent permitted by applicable
          law.
        </p>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-base font-semibold">7. Changes to these terms</h2>
        <p className="text-muted-foreground">
          We may update these terms as the product evolves. Continued use of the
          platform after changes are posted constitutes acceptance of the
          revised terms.
        </p>
      </section>
    </div>
  )
}
