import { Suspense } from "react"

import { FeedPageClient } from "./page.client"

export default function FeedPage() {
  // `FeedPageClient` reads `useSearchParams` for URL-synced filter state, which
  // requires a Suspense boundary on the App Router. The fallback mirrors the
  // page chrome height so layout doesn't jump on first paint.
  return (
    <Suspense fallback={<div className="h-96" />}>
      <FeedPageClient />
    </Suspense>
  )
}
