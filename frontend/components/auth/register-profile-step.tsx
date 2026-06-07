"use client"

import { useTranslations } from "next-intl"

import { LocationField } from "@/components/forms/location-field"
import { TextareaField } from "@/components/forms/textarea-field"
import { TextField } from "@/components/forms/text-field"
import type { RegisterFormValues } from "@/lib/auth/schemas"

export function RegisterProfileStep() {
  const t = useTranslations("auth.register")

  return (
    <div className="grid gap-4">
      <TextField<RegisterFormValues>
        name="displayName"
        label={t("displayNameLabel")}
        autoComplete="name"
      />

      <TextField<RegisterFormValues>
        name="workplace"
        label={t("workplaceLabel")}
        optional
      />

      <TextField<RegisterFormValues>
        name="position"
        label={t("positionLabel")}
        optional
      />

      <LocationField<RegisterFormValues>
        name="location"
        label={t("locationLabel")}
        placeholder={t("locationPlaceholder")}
      />

      <TextareaField<RegisterFormValues>
        name="bio"
        label={t("bioLabel")}
        rows={3}
        optional
      />
    </div>
  )
}
