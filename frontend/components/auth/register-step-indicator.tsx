"use client"

import type { RegistrationStep } from "@/lib/auth/functions"

export function RegisterStepIndicator({ step }: { step: RegistrationStep }) {
  return (
    <div className="grid grid-cols-2 gap-2 text-center text-xs font-medium">
      <span
        className={
          step === "account"
            ? "rounded-md bg-primary px-3 py-2 text-primary-foreground"
            : "rounded-md bg-muted px-3 py-2 text-muted-foreground"
        }
      >
        Account
      </span>
      <span
        className={
          step === "profile"
            ? "rounded-md bg-primary px-3 py-2 text-primary-foreground"
            : "rounded-md bg-muted px-3 py-2 text-muted-foreground"
        }
      >
        Profile
      </span>
    </div>
  )
}
