"use client"

import Link from "next/link"
import { useTranslations } from "next-intl"

import { CheckboxField } from "@/components/forms/checkbox-field"
import { PasswordField } from "@/components/forms/password-field"
import { TextField } from "@/components/forms/text-field"
import { APP_LEGAL_ROUTES } from "@/lib/auth/constants"
import type { RegisterFormValues } from "@/lib/auth/schemas"

export function RegisterAccountStep() {
  const t = useTranslations("auth.register")

  return (
    <div className="grid gap-4">
      <TextField<RegisterFormValues>
        name="email"
        label={t("emailLabel")}
        type="email"
        autoComplete="email"
        placeholder={t("emailPlaceholder")}
      />

      <PasswordField<RegisterFormValues>
        name="password"
        label={t("passwordLabel")}
        autoComplete="new-password"
        description={t("passwordDescription")}
      />

      <PasswordField<RegisterFormValues>
        name="confirmPassword"
        label={t("confirmPasswordLabel")}
        autoComplete="new-password"
      />

      <CheckboxField<RegisterFormValues>
        name="acceptTerms"
        label={
          <>
            {t("acceptTermsPreamble")}{" "}
            <Link
              href={APP_LEGAL_ROUTES.terms}
              className="font-medium text-foreground underline-offset-4 hover:underline"
            >
              {t("acceptTermsLink")}
            </Link>
            .
          </>
        }
      />
    </div>
  )
}
