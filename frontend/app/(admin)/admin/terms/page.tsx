import type { Metadata } from "next"

import { TermsManager } from "@/components/admin/terms-manager"

export const metadata: Metadata = { title: "Terms – Admin" }

export default function AdminTermsPage() {
  return <TermsManager />
}
