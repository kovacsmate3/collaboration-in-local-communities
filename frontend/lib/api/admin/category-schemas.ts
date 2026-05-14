import { z } from "zod"
import { ICON_REGISTRY } from "@/lib/icon-registry"

const ALLOWED_ICONS = Object.keys(ICON_REGISTRY) as [string, ...string[]]

export const createCategorySchema = z.object({
  code: z
    .string()
    .min(1, "Code is required")
    .max(64, "Code must be 64 characters or fewer")
    .regex(
      /^[a-z0-9_-]+$/,
      "Code may only contain lowercase letters, numbers, hyphens, and underscores"
    ),
  name: z
    .string()
    .min(1, "Name is required")
    .max(120, "Name must be 120 characters or fewer"),
  icon: z.enum(ALLOWED_ICONS, {
    message: "Icon must be from the approved list",
  }),
  description: z
    .string()
    .max(500, "Description must be 500 characters or fewer")
    .optional()
    .or(z.literal("")),
  sortOrder: z.coerce
    .number()
    .int("Sort order must be a whole number")
    .min(0, "Sort order must be 0 or greater"),
})

export const updateCategorySchema = createCategorySchema.omit({ code: true })

export type CreateCategoryFormValues = z.infer<typeof createCategorySchema>
export type UpdateCategoryFormValues = z.infer<typeof updateCategorySchema>
