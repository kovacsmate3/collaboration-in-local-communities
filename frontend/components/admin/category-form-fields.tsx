"use client"

import { Loading03Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"
import { useFormContext } from "react-hook-form"

import { Button } from "@/components/ui/button"
import { DialogFooter } from "@/components/ui/dialog"
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { IconPicker } from "@/components/admin/icon-picker"

interface CategoryFormValues {
  code?: string
  name: string
  icon: string
  description?: string
  sortOrder: number
}

interface CategoryFormFieldsProps {
  includeCode?: boolean
  namePlaceholder?: string
  descriptionPlaceholder?: string
}

export function CategoryFormFields({
  includeCode = false,
  namePlaceholder,
  descriptionPlaceholder,
}: CategoryFormFieldsProps) {
  const { control } = useFormContext<CategoryFormValues>()

  return (
    <>
      {includeCode ? (
        <FormField
          control={control}
          name="code"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Code</FormLabel>
              <FormControl>
                <Input
                  id={field.name}
                  placeholder="e.g. garden_work"
                  {...field}
                />
              </FormControl>
              <FormDescription>
                Lowercase letters, numbers, hyphens, underscores. Immutable
                after creation.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
      ) : null}

      <FormField
        control={control}
        name="name"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Name</FormLabel>
            <FormControl>
              <Input id={field.name} placeholder={namePlaceholder} {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="icon"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Icon</FormLabel>
            <FormControl>
              <IconPicker value={field.value} onChange={field.onChange} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="description"
        render={({ field }) => (
          <FormItem>
            <FormLabel>
              Description{" "}
              <span className="font-normal text-muted-foreground">
                (optional)
              </span>
            </FormLabel>
            <FormControl>
              <Textarea
                id={field.name}
                placeholder={descriptionPlaceholder}
                className="resize-none"
                rows={2}
                {...field}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="sortOrder"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Sort order</FormLabel>
            <FormControl>
              <Input id={field.name} type="number" min={0} {...field} />
            </FormControl>
            <FormDescription>Lower numbers appear first.</FormDescription>
            <FormMessage />
          </FormItem>
        )}
      />
    </>
  )
}

interface CategoryFormActionsProps {
  isPending: boolean
  onCancel: () => void
  submitLabel: string
}

export function CategoryFormActions({
  isPending,
  onCancel,
  submitLabel,
}: CategoryFormActionsProps) {
  return (
    <DialogFooter>
      <Button
        type="button"
        variant="outline"
        onClick={onCancel}
        disabled={isPending}
      >
        Cancel
      </Button>
      <Button type="submit" disabled={isPending}>
        {isPending ? (
          <HugeiconsIcon
            icon={Loading03Icon}
            className="size-4 animate-spin"
            strokeWidth={2}
          />
        ) : null}
        {submitLabel}
      </Button>
    </DialogFooter>
  )
}
