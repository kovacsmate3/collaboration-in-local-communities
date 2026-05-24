import { Suspense } from "react"
import type { Metadata } from "next"

import { ResetPasswordForm } from "@/components/auth/reset-password-form"

export const metadata: Metadata = {
  title: "Reset your password",
}

export default function ResetPasswordPage() {
  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">
          Reset your password
        </h1>
        <p className="text-sm text-muted-foreground">
          Enter a new password for your account.
        </p>
      </header>
      <Suspense>
        <ResetPasswordForm />
      </Suspense>
    </div>
  )
}
