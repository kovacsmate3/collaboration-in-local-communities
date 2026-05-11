"use client"

import { LocationField } from "@/components/forms/location-field"
import { TextareaField } from "@/components/forms/textarea-field"
import { TextField } from "@/components/forms/text-field"
import type { RegisterFormValues } from "@/lib/auth/schemas"

export function RegisterProfileStep() {
  return (
    <div className="grid gap-4">
      <TextField<RegisterFormValues>
        name="displayName"
        label="Full name"
        autoComplete="name"
      />

      <TextField<RegisterFormValues>
        name="workplace"
        label="Workplace / school"
        optional
      />

      <TextField<RegisterFormValues> name="position" label="Role" optional />

      <LocationField<RegisterFormValues>
        name="location"
        label="Location"
        placeholder="City, neighbourhood, or street address"
      />

      <TextareaField<RegisterFormValues>
        name="bio"
        label="Short bio"
        rows={3}
        optional
      />
    </div>
  )
}
