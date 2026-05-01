import type { Metadata } from "next"
import Link from "next/link"

import { APP_NAME } from "@/lib/constants"

export const metadata: Metadata = {
  title: "Terms & Conditions",
}

export default function TermsPage() {
  return (
    <div className="mx-auto max-w-2xl px-6 py-16">
      <Link
        href="/register"
        className="mb-8 inline-block text-sm text-muted-foreground hover:text-foreground"
      >
        ← Back to registration
      </Link>

      <h1 className="mb-2 text-3xl font-semibold tracking-tight">
        Terms &amp; Conditions
      </h1>
      <p className="mb-10 text-sm text-muted-foreground">
        Last updated: May 2025 &mdash; placeholder, not legally binding.
      </p>

      <div className="prose prose-neutral dark:prose-invert flex flex-col gap-8 text-sm leading-relaxed">
        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">1. About {APP_NAME}</h2>
          <p className="text-muted-foreground">
            {APP_NAME} is a platform that connects neighbours who need help with
            those willing to offer it. By creating an account you agree to use
            the service in good faith and in accordance with your local laws.
          </p>
        </section>

        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">2. Your account</h2>
          <p className="text-muted-foreground">
            You are responsible for keeping your credentials secure and for all
            activity that occurs under your account. Notify us immediately if
            you suspect unauthorised access.
          </p>
        </section>

        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">3. Acceptable use</h2>
          <p className="text-muted-foreground">
            You may not use {APP_NAME} to post illegal tasks, harass other
            users, misrepresent your identity, or scrape the platform without
            written permission. We reserve the right to suspend accounts that
            violate these rules.
          </p>
        </section>

        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">4. Payments &amp; barter</h2>
          <p className="text-muted-foreground">
            {APP_NAME} facilitates agreements between users but is not a party
            to them. Any payment or exchange is solely between the Seeker and
            the Helper. We are not liable for disputes arising from tasks.
          </p>
        </section>

        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">5. Privacy</h2>
          <p className="text-muted-foreground">
            We collect only the information needed to operate the service. We do
            not sell your data. A full privacy policy will be published before
            the public launch.
          </p>
        </section>

        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">
            6. Limitation of liability
          </h2>
          <p className="text-muted-foreground">
            {APP_NAME} is provided &ldquo;as is&rdquo; during this early phase.
            We make no warranties about uptime or fitness for a particular
            purpose. Our liability is limited to the maximum extent permitted by
            applicable law.
          </p>
        </section>

        <section className="flex flex-col gap-2">
          <h2 className="text-base font-semibold">7. Changes to these terms</h2>
          <p className="text-muted-foreground">
            We may update these terms as the product evolves. Continued use of
            the platform after changes are posted constitutes acceptance of the
            revised terms.
          </p>
        </section>
      </div>
    </div>
  )
}
