"use client"

import Link from "next/link"

import { CheckboxField } from "@/components/forms/checkbox-field"
import { PasswordField } from "@/components/forms/password-field"
import { TextField } from "@/components/forms/text-field"
import { APP_LEGAL_ROUTES } from "@/lib/auth/constants"
import type { RegisterFormValues } from "@/lib/auth/schemas"

export function RegisterAccountStep() {
  return (
    <div className="grid gap-4">
      <TextField<RegisterFormValues>
        name="email"
        label="Email"
        type="email"
        autoComplete="email"
        placeholder="you@example.com"
      />

      <PasswordField<RegisterFormValues>
        name="password"
        label="Password"
        autoComplete="new-password"
        description="Use at least 8 characters with uppercase, lowercase, number, and symbol."
      />

      <CheckboxField<RegisterFormValues>
        name="acceptTerms"
        label={
          <>
            I agree to the{" "}
            <Link
              href={APP_LEGAL_ROUTES.terms}
              className="font-medium text-foreground underline-offset-4 hover:underline"
            >
              Terms
            </Link>
            .
          </>
        }
      />
    </div>
  )
}
