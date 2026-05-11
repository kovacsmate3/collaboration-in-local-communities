import { toOptionalString } from "@/lib/auth/functions"
import type { RegisterInput } from "@/lib/auth/types"
import type { RegisterFormValues } from "@/lib/auth/schemas"

export function toRegisterInput(values: RegisterFormValues): RegisterInput {
  const { latitude, longitude } = values.location

  return {
    email: values.email,
    password: values.password,
    acceptTerms: values.acceptTerms,
    displayName: values.displayName,
    workplace: toOptionalString(values.workplace),
    position: toOptionalString(values.position),
    locationText: toOptionalString(values.location.locationText),
    latitude,
    longitude,
    bio: toOptionalString(values.bio),
  }
}
