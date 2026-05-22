"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import type { SubmitEvent } from "react"
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
import { useCreateTask, useUpdateTask } from "@/lib/api/tasks"
import { COMPENSATION_LABELS } from "@/lib/constants"
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
  compensationType: "voluntary",
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
  const isEditing = Boolean(taskId)
  const { user } = useAuth()
  const router = useRouter()
  const { data: categories = [], isLoading: categoriesLoading } =
    useCategories()
  const { mutate: createTask, isPending: isCreating } = useCreateTask()
  const { mutate: updateTask, isPending: isUpdating } = useUpdateTask(
    taskId ?? ""
  )
  const isPending = isCreating || isUpdating
  const [form, setForm] = React.useState<PostTaskFormState>({
    ...INITIAL,
    ...initialValues,
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
      toast.success("Verification email sent — check your inbox.")
    } catch {
      toast.error("Could not send the email. Please try again.")
    } finally {
      setIsResending(false)
    }
  }

  if (!isEditing && user && !user.emailVerified) {
    return (
      <div className="rounded-md border border-amber-200 bg-amber-50 px-5 py-4 dark:border-amber-900 dark:bg-amber-950">
        <p className="font-medium text-amber-800 dark:text-amber-200">
          Email verification required
        </p>
        <p className="mt-1 text-sm text-amber-700 dark:text-amber-300">
          You need to verify your email address before posting tasks. Check your
          inbox for the verification link.
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          <Button
            size="sm"
            variant="outline"
            disabled={isResending}
            onClick={handleResend}
            className="border-amber-300 bg-transparent text-amber-800 hover:bg-amber-100 dark:border-amber-700 dark:text-amber-200 dark:hover:bg-amber-900"
          >
            {isResending ? "Sending..." : "Resend verification email"}
          </Button>
          <Button size="sm" variant="ghost" asChild>
            <Link href={APP_AUTH_ROUTES.verifyEmail}>Already verified?</Link>
          </Button>
        </div>
      </div>
    )
  }

  function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()

    const plainText = form.description.replace(/<[^>]*>/g, "").trim()
    if (plainText.length < 10) {
      toast.error("Description must be at least 10 characters.")
      return
    }
    if (form.description.length > 50_000) {
      toast.error("Description is too long. Please shorten it.")
      return
    }

    const amount =
      form.compensationType === "paid" && form.compensationAmount
        ? Number(form.compensationAmount)
        : undefined

    if (isEditing) {
      updateTask(
        {
          title: form.title,
          description: form.description,
          categoryId: form.categoryId,
          compensationType: form.compensationType,
          compensationAmount: amount,
          locationText: form.location.locationText || undefined,
        },
        {
          onSuccess: () => {
            toast.success("Task updated!")
            router.push(`/tasks/${taskId}`)
          },
          onError: (err) => {
            const message =
              err instanceof Error ? err.message : "Could not update the task."
            toast.error(message)
          },
        }
      )
      return
    }

    createTask(
      {
        title: form.title,
        description: form.description,
        categoryId: form.categoryId,
        compensationType: form.compensationType,
        compensationAmount: amount,
        locationText: form.location.locationText || undefined,
        latitude: form.location.latitude,
        longitude: form.location.longitude,
      },
      {
        onSuccess: (task) => {
          toast.success("Task posted!")
          router.push(`/tasks/${task.id}`)
        },
        onError: (err) => {
          const message =
            err instanceof Error ? err.message : "Could not post the task."
          toast.error(message)
        },
      }
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <Label htmlFor="title">Task title</Label>
        <Input
          id="title"
          required
          maxLength={160}
          placeholder="e.g. Help carrying boxes to my new apartment"
          value={form.title}
          onChange={(e) => update("title", e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label>Description</Label>
        <RichTextEditor
          value={form.description}
          onChange={(html) => update("description", html)}
          placeholder="Describe what you need help with, when, and any constraints."
          maxLength={50_000}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="category">Category</Label>
          <Select
            required
            value={form.categoryId}
            onValueChange={(v) => update("categoryId", v)}
            disabled={categoriesLoading}
          >
            <SelectTrigger id="category">
              <SelectValue
                placeholder={categoriesLoading ? "Loading…" : "Pick a category"}
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
            label="Location"
            placeholder="District, neighbourhood, or street address"
            value={form.location}
            onChange={(location) => update("location", location)}
          />
        </div>
      </div>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Compensation</legend>
        <RadioGroup
          value={form.compensationType}
          onValueChange={(v) =>
            update("compensationType", v as CompensationType)
          }
          className="grid gap-2 sm:grid-cols-3"
        >
          {(Object.keys(COMPENSATION_LABELS) as CompensationType[]).map(
            (key) => (
              <Label
                key={key}
                htmlFor={`comp-${key}`}
                className="flex cursor-pointer items-center gap-2 rounded-md border border-border bg-background p-3 hover:bg-muted"
              >
                <RadioGroupItem id={`comp-${key}`} value={key} />
                <span className="text-sm font-medium">
                  {COMPENSATION_LABELS[key]}
                </span>
              </Label>
            )
          )}
        </RadioGroup>

        {form.compensationType === "paid" ? (
          <div className="flex flex-col gap-2">
            <Label htmlFor="amount">Amount (HUF)</Label>
            <Input
              id="amount"
              type="number"
              min={0}
              required
              placeholder="e.g. 5000"
              value={form.compensationAmount}
              onChange={(e) => update("compensationAmount", e.target.value)}
            />
          </div>
        ) : null}
      </fieldset>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" asChild>
          <Link href={isEditing ? `/tasks/${taskId}` : "/feed"}>Cancel</Link>
        </Button>
        <Button type="submit" disabled={isPending || !form.categoryId}>
          {isEditing
            ? isPending
              ? "Saving…"
              : "Save changes"
            : isPending
              ? "Posting…"
              : "Post task"}
        </Button>
      </div>
    </form>
  )
}
