"use client"

import * as React from "react"

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
import { Textarea } from "@/components/ui/textarea"
import {
  COMPENSATION_LABELS,
  TASK_CATEGORIES,
  TASK_CATEGORY_LIST,
} from "@/lib/constants"
import type { CompensationType, TaskCategory } from "@/lib/types"

interface PostTaskFormState {
  title: string
  description: string
  category: TaskCategory | ""
  location: string
  compensationType: CompensationType
  compensationAmount: string
  barterOffer: string
}

const INITIAL: PostTaskFormState = {
  title: "",
  description: "",
  category: "",
  location: "",
  compensationType: "voluntary",
  compensationAmount: "",
  barterOffer: "",
}

/**
 * Skeleton task-creation form. Hooks up local state only; replace the
 * submit handler with a server action / mutation when the API is ready.
 *
 * Validation is intentionally minimal here - production should use a
 * schema layer (e.g. zod) so backend and frontend stay aligned.
 */
export function PostTaskForm() {
  const [form, setForm] = React.useState<PostTaskFormState>(INITIAL)

  const update = <K extends keyof PostTaskFormState>(
    key: K,
    value: PostTaskFormState[K]
  ) => setForm((prev) => ({ ...prev, [key]: value }))

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    // TODO: replace with real submission once the API exists.
    // eslint-disable-next-line no-console
    console.log("Submit task:", form)
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <Label htmlFor="title">Task title</Label>
        <Input
          id="title"
          required
          maxLength={120}
          placeholder="e.g. Help carrying boxes to my new apartment"
          value={form.title}
          onChange={(e) => update("title", e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          required
          rows={5}
          placeholder="Describe what you need help with, when, and any constraints."
          value={form.description}
          onChange={(e) => update("description", e.target.value)}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="category">Category</Label>
          <Select
            value={form.category}
            onValueChange={(v) => update("category", v as TaskCategory)}
          >
            <SelectTrigger id="category">
              <SelectValue placeholder="Pick a category" />
            </SelectTrigger>
            <SelectContent>
              {TASK_CATEGORY_LIST.map((c) => (
                <SelectItem key={c} value={c}>
                  {TASK_CATEGORIES[c].label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-2">
          <Label htmlFor="location">Location</Label>
          <Input
            id="location"
            required
            placeholder="District, neighbourhood, or 'Online'"
            value={form.location}
            onChange={(e) => update("location", e.target.value)}
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
              placeholder="e.g. 5000"
              value={form.compensationAmount}
              onChange={(e) => update("compensationAmount", e.target.value)}
            />
          </div>
        ) : null}

        {form.compensationType === "barter" ? (
          <div className="flex flex-col gap-2">
            <Label htmlFor="barter">What do you offer in exchange?</Label>
            <Input
              id="barter"
              placeholder="e.g. Coffee + lunch / Help with my Spanish homework"
              value={form.barterOffer}
              onChange={(e) => update("barterOffer", e.target.value)}
            />
          </div>
        ) : null}
      </fieldset>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost">
          Cancel
        </Button>
        <Button type="submit">Post task</Button>
      </div>
    </form>
  )
}
