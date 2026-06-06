"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import type { SubmitEvent } from "react"
import { useTranslations } from "next-intl"
import { toast } from "sonner"

import { LocationInput } from "@/components/shared/location-input"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { RichTextEditor } from "@/components/shared/rich-text-editor"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES } from "@/lib/auth/constants"
import { resendVerificationEmail } from "@/lib/auth/functions"
import { useCategories } from "@/lib/api/categories"
import { useOwnProfile, type OwnProfileResponse } from "@/lib/api/profile"
import { useCreateTask, useUpdateTask } from "@/lib/api/tasks"
import { COMPENSATION_OPTIONS } from "@/lib/constants"
import type { LocationValue } from "@/lib/location"
import type { CompensationType } from "@/lib/types"

interface PostTaskFormState {
  title: string
  description: string
  categoryId: string
  location: LocationValue
  compensationType: CompensationType
  compensationAmount: string
}

const INITIAL: PostTaskFormState = {
  title: "",
  description: "",
  categoryId: "",
  location: { locationText: "" },
  compensationType: "points",
  compensationAmount: "",
}

interface PostTaskFormProps {
  taskId?: string
  initialValues?: Partial<PostTaskFormState>
}

export function PostTaskForm({
  taskId,
  initialValues,
}: PostTaskFormProps = {}) {
  const t = useTranslations("tasks.post")
  const tCompensation = useTranslations("tasks.compensation")
  const isEditing = Boolean(taskId)
  const { user } = useAuth()
  const router = useRouter()
  const { data: categories = [], isLoading: categoriesLoading } =
    useCategories()
  const { data: profile } = useOwnProfile(Boolean(user))
  const { mutate: createTask, isPending: isCreating } = useCreateTask()
  const { mutate: updateTask, isPending: isUpdating } = useUpdateTask(
    taskId ?? ""
  )
  const isPending = isCreating || isUpdating
  const [form, setForm] = React.useState<PostTaskFormState>(() => {
    const next = { ...INITIAL, ...initialValues }
    return {
      ...next,
      compensationType: normalizeCompensationType(next.compensationType),
    }
  })
  const [isResending, setIsResending] = React.useState(false)

  const update = <K extends keyof PostTaskFormState>(
    key: K,
    value: PostTaskFormState[K]
  ) => setForm((prev) => ({ ...prev, [key]: value }))

  async function handleResend() {
    if (!user?.email) return
    setIsResending(true)
    try {
      await resendVerificationEmail(user.email)
      toast.success(t("resendSent"))
    } catch {
      toast.error(t("resendError"))
    } finally {
      setIsResending(false)
    }
  }

  if (!isEditing && user && !user.emailVerified) {
    return (
      <div className="rounded-md border border-amber-200 bg-amber-50 px-5 py-4 dark:border-amber-900 dark:bg-amber-950">
        <p className="font-medium text-amber-800 dark:text-amber-200">
          {t("emailGateTitle")}
        </p>
        <p className="mt-1 text-sm text-amber-700 dark:text-amber-300">
          {t("emailGateBody")}
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          <Button
            size="sm"
            variant="outline"
            disabled={isResending}
            onClick={handleResend}
            className="border-amber-300 bg-transparent text-amber-800 hover:bg-amber-100 dark:border-amber-700 dark:text-amber-200 dark:hover:bg-amber-900"
          >
            {isResending ? t("resending") : t("resend")}
          </Button>
          <Button size="sm" variant="ghost" asChild>
            <Link href={APP_AUTH_ROUTES.verifyEmail}>
              {t("alreadyVerified")}
            </Link>
          </Button>
        </div>
      </div>
    )
  }

  function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()

    const title = form.title.trim()
    if (title.length < 3) {
      toast.error(t("errorTitleMin"))
      return
    }

    const plainText = form.description.replace(/<[^>]*>/g, "").trim()
    if (plainText.length < 10) {
      toast.error(t("errorDescriptionMin"))
      return
    }
    if (form.description.length > 50_000) {
      toast.error(t("errorDescriptionMax"))
      return
    }

    if (!form.categoryId) {
      toast.error(t("errorCategory"))
      return
    }

    const amount =
      form.compensationType === "paid" && form.compensationAmount
        ? Number(form.compensationAmount)
        : undefined

    if (
      form.compensationType === "paid" &&
      (amount === undefined || !Number.isFinite(amount) || amount < 0)
    ) {
      toast.error(t("errorAmount"))
      return
    }

    const location = getSubmissionLocation(form.location, profile)

    if (isEditing) {
      updateTask(
        {
          title,
          description: form.description,
          categoryId: form.categoryId,
          compensationType: form.compensationType,
          compensationAmount: amount,
          locationText: location.locationText || undefined,
          latitude: location.latitude,
          longitude: location.longitude,
        },
        {
          onSuccess: () => {
            toast.success(t("updatedToast"))
            router.push(`/tasks/${taskId}`)
          },
          onError: (err) => {
            const message =
              err instanceof Error ? err.message : t("updateErrorDefault")
            toast.error(message)
          },
        }
      )
      return
    }

    createTask(
      {
        title,
        description: form.description,
        categoryId: form.categoryId,
        compensationType: form.compensationType,
        compensationAmount: amount,
        locationText: location.locationText || undefined,
        latitude: location.latitude,
        longitude: location.longitude,
      },
      {
        onSuccess: (task) => {
          toast.success(t("postedToast"))
          router.push(`/tasks/${task.id}`)
        },
        onError: (err) => {
          const message =
            err instanceof Error ? err.message : t("postErrorDefault")
          toast.error(message)
        },
      }
    )
  }

  const submitLabel = isEditing
    ? isPending
      ? t("submitSaving")
      : t("submitSave")
    : isPending
      ? t("submitPosting")
      : t("submitPost")

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <Label htmlFor="title">{t("titleLabel")}</Label>
        <Input
          id="title"
          required
          minLength={3}
          maxLength={160}
          placeholder={t("titlePlaceholder")}
          value={form.title}
          onChange={(e) => update("title", e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label>{t("descriptionLabel")}</Label>
        <RichTextEditor
          value={form.description}
          onChange={(html) => update("description", html)}
          placeholder={t("descriptionPlaceholder")}
          maxLength={50_000}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="category">{t("categoryLabel")}</Label>
          <Select
            required
            value={form.categoryId}
            onValueChange={(v) => update("categoryId", v)}
            disabled={categoriesLoading}
          >
            <SelectTrigger id="category">
              <SelectValue
                placeholder={
                  categoriesLoading
                    ? t("categoryLoading")
                    : t("categoryPlaceholder")
                }
              />
            </SelectTrigger>
            <SelectContent>
              {categories.map((c) => (
                <SelectItem key={c.id} value={c.id}>
                  {c.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-2">
          <LocationInput
            id="location"
            label={t("locationLabel")}
            placeholder={t("locationPlaceholder")}
            value={form.location}
            onChange={(location) => update("location", location)}
          />
        </div>
      </div>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">{t("rewardLegend")}</legend>
        <RadioGroup
          value={form.compensationType}
          onValueChange={(v) =>
            update("compensationType", v as CompensationType)
          }
          className="grid gap-2 sm:grid-cols-3"
        >
          {COMPENSATION_OPTIONS.map((value) => (
            <Label
              key={value}
              htmlFor={`comp-${value}`}
              className="flex cursor-pointer items-center gap-2 rounded-md border border-border bg-background p-3 hover:bg-muted"
            >
              <RadioGroupItem id={`comp-${value}`} value={value} />
              <span className="text-sm font-medium">
                {tCompensation(value)}
              </span>
            </Label>
          ))}
        </RadioGroup>

        {form.compensationType === "paid" ? (
          <div className="flex flex-col gap-2">
            <Label htmlFor="amount">{t("amountLabel")}</Label>
            <Input
              id="amount"
              type="number"
              min={0}
              max={9_999_999_999.99}
              step="0.01"
              required
              placeholder={t("amountPlaceholder")}
              value={form.compensationAmount}
              onChange={(e) => update("compensationAmount", e.target.value)}
            />
          </div>
        ) : null}
      </fieldset>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" asChild>
          <Link href={isEditing ? `/tasks/${taskId}` : "/feed"}>
            {t("cancel")}
          </Link>
        </Button>
        <Button type="submit" disabled={isPending || !form.categoryId}>
          {submitLabel}
        </Button>
      </div>
    </form>
  )
}

function getSubmissionLocation(
  location: LocationValue,
  profile: OwnProfileResponse | undefined
): LocationValue {
  const locationText = location.locationText.trim()
  if (locationText) {
    return { ...location, locationText }
  }

  const profileLocationText = profile?.locationText?.trim() ?? ""
  if (!profileLocationText) {
    return { locationText: "" }
  }

  const latitude = toFiniteCoordinate(profile?.latitude)
  const longitude = toFiniteCoordinate(profile?.longitude)
  if (latitude === undefined || longitude === undefined) {
    return { locationText: profileLocationText }
  }

  return { locationText: profileLocationText, latitude, longitude }
}

function toFiniteCoordinate(value: number | null | undefined) {
  return typeof value === "number" && Number.isFinite(value) ? value : undefined
}

function normalizeCompensationType(value: unknown): CompensationType {
  if (typeof value !== "string") return "points"

  switch (value.toLowerCase()) {
    case "paid":
      return "paid"
    case "barter":
      return "barter"
    case "points":
    case "voluntary":
      return "points"
    default:
      return "points"
  }
}
