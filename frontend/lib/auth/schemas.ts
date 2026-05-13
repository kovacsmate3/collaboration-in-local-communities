import { z } from "zod"

const passwordRequirements = [
  {
    test: /[a-z]/,
    message: "Include at least one lowercase letter.",
  },
  {
    test: /[A-Z]/,
    message: "Include at least one uppercase letter.",
  },
  {
    test: /\d/,
    message: "Include at least one number.",
  },
  {
    test: /[^A-Za-z0-9]/,
    message: "Include at least one symbol.",
  },
] as const

const emailSchema = z
  .string()
  .trim()
  .min(1, "Enter your email address.")
  .email("Enter a valid email address.")

const registerPasswordSchema = passwordRequirements.reduce(
  (schema, requirement) => schema.regex(requirement.test, requirement.message),
  z
    .string()
    .min(1, "Create a password.")
    .min(8, "Password must be at least 8 characters.")
)

const locationSchema = z
  .object({
    locationText: z.string().trim(),
    latitude: z.number().finite().optional(),
    longitude: z.number().finite().optional(),
  })
  .superRefine((location, ctx) => {
    if (location.locationText.length > 300) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Location must be 300 characters or fewer.",
      })
    }

    if (
      (location.latitude === undefined) !==
      (location.longitude === undefined)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Latitude and longitude must be provided together.",
      })
      return
    }

    if (
      location.latitude !== undefined &&
      (location.latitude < -90 || location.latitude > 90)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Latitude must be between -90 and 90.",
      })
    }

    if (
      location.longitude !== undefined &&
      (location.longitude < -180 || location.longitude > 180)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Longitude must be between -180 and 180.",
      })
    }
  })

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "Enter your password."),
})

export const registerSchema = z.object({
  email: emailSchema,
  password: registerPasswordSchema,
  acceptTerms: z.boolean().refine((accepted) => accepted, {
    message: "Accept the terms to create an account.",
  }),
  displayName: z
    .string()
    .trim()
    .min(1, "Enter your full name.")
    .max(120, "Full name must be 120 characters or fewer."),
  workplace: z
    .string()
    .trim()
    .max(200, "Workplace or school must be 200 characters or fewer."),
  position: z.string().trim().max(200, "Role must be 200 characters or fewer."),
  location: locationSchema,
  bio: z.string().trim().max(1000, "Bio must be 1000 characters or fewer."),
})

export type LoginFormValues = z.infer<typeof loginSchema>
export type RegisterFormValues = z.infer<typeof registerSchema>

export const LOGIN_FORM_DEFAULT_VALUES: LoginFormValues = {
  email: "",
  password: "",
}

export const REGISTER_FORM_DEFAULT_VALUES: RegisterFormValues = {
  email: "",
  password: "",
  acceptTerms: false,
  displayName: "",
  workplace: "",
  position: "",
  location: { locationText: "" },
  bio: "",
}

export const REGISTER_ACCOUNT_FIELDS = [
  "email",
  "password",
  "acceptTerms",
] as const
