"use client"

import * as React from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { toast } from "sonner"
import { HugeiconsIcon } from "@hugeicons/react"
import { Loading03Icon } from "@hugeicons/core-free-icons"

import {
  createCategorySchema,
  updateCategorySchema,
  type CreateCategoryFormValues,
  type UpdateCategoryFormValues,
} from "@/lib/api/admin/category-schemas"
import {
  useCreateCategory,
  useUpdateCategory,
  type AdminCategoryResponse,
} from "@/lib/api/admin/categories"
import { ApiError } from "@/lib/api/client"
import { ICON_REGISTRY } from "@/lib/icon-registry"

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog"
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { IconPicker } from "./icon-picker"

// ── Create dialog ─────────────────────────────────────────────────────────────

interface CreateCategoryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateCategoryDialog({
  open,
  onOpenChange,
}: CreateCategoryDialogProps) {
  const { mutateAsync, isPending } = useCreateCategory()

  const form = useForm<CreateCategoryFormValues>({
    resolver: zodResolver(createCategorySchema),
    defaultValues: {
      code: "",
      name: "",
      icon: Object.keys(ICON_REGISTRY)[0] ?? "",
      description: "",
      sortOrder: 0,
    },
  })

  async function onSubmit(values: CreateCategoryFormValues) {
    try {
      await mutateAsync({
        ...values,
        description: values.description || undefined,
      })
      toast.success(`Category "${values.name}" created`)
      form.reset()
      onOpenChange(false)
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        form.setError("code", {
          message: `Code "${values.code}" is already in use`,
        })
      } else {
        toast.error(
          err instanceof Error ? err.message : "Failed to create category"
        )
      }
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!isPending) onOpenChange(o)
      }}
    >
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Create category</DialogTitle>
          <DialogDescription>
            Add a new task category to the marketplace. The code is permanent
            and cannot be changed after creation.
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="code"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Code</FormLabel>
                  <FormControl>
                    <div>
                      <Input
                        id={field.name}
                        placeholder="e.g. garden_work"
                        {...field}
                      />
                    </div>
                  </FormControl>
                  <FormDescription>
                    Lowercase letters, numbers, hyphens, underscores. Immutable
                    after creation.
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Name</FormLabel>
                  <FormControl>
                    <div>
                      <Input
                        id={field.name}
                        placeholder="e.g. Garden Work"
                        {...field}
                      />
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="icon"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Icon</FormLabel>
                  <FormControl>
                    <div>
                      <IconPicker
                        value={field.value}
                        onChange={field.onChange}
                      />
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
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
                    <div>
                      <Textarea
                        id={field.name}
                        placeholder="Brief description for seekers…"
                        className="resize-none"
                        rows={2}
                        {...field}
                      />
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="sortOrder"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Sort order</FormLabel>
                  <FormControl>
                    <div>
                      <Input id={field.name} type="number" min={0} {...field} />
                    </div>
                  </FormControl>
                  <FormDescription>Lower numbers appear first.</FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isPending}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending && (
                  <HugeiconsIcon
                    icon={Loading03Icon}
                    className="size-4 animate-spin"
                    strokeWidth={2}
                  />
                )}
                Create category
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}

// ── Edit dialog ───────────────────────────────────────────────────────────────

interface EditCategoryDialogProps {
  category: AdminCategoryResponse | null
  onOpenChange: (open: boolean) => void
}

export function EditCategoryDialog({
  category,
  onOpenChange,
}: EditCategoryDialogProps) {
  const { mutateAsync, isPending } = useUpdateCategory()

  const form = useForm<UpdateCategoryFormValues>({
    resolver: zodResolver(updateCategorySchema),
    values: category
      ? {
          name: category.name,
          icon: category.icon,
          description: category.description ?? "",
          sortOrder: category.sortOrder,
        }
      : undefined,
  })

  async function onSubmit(values: UpdateCategoryFormValues) {
    if (!category) return
    try {
      await mutateAsync({
        id: category.id,
        data: {
          ...values,
          description: values.description || undefined,
        },
      })
      toast.success(`Category "${values.name}" updated`)
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to update category"
      )
    }
  }

  return (
    <Dialog
      open={Boolean(category)}
      onOpenChange={(o) => {
        if (!isPending) onOpenChange(o)
      }}
    >
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit category</DialogTitle>
          <DialogDescription>
            Update the category details. The code{" "}
            <Badge variant="secondary" className="font-mono text-xs">
              {category?.code}
            </Badge>{" "}
            is permanent and cannot be changed.
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Name</FormLabel>
                  <FormControl>
                    <div>
                      <Input id={field.name} {...field} />
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="icon"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Icon</FormLabel>
                  <FormControl>
                    <div>
                      <IconPicker
                        value={field.value}
                        onChange={field.onChange}
                      />
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
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
                    <div>
                      <Textarea
                        id={field.name}
                        className="resize-none"
                        rows={2}
                        {...field}
                      />
                    </div>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="sortOrder"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Sort order</FormLabel>
                  <FormControl>
                    <div>
                      <Input id={field.name} type="number" min={0} {...field} />
                    </div>
                  </FormControl>
                  <FormDescription>Lower numbers appear first.</FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isPending}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isPending}>
                {isPending && (
                  <HugeiconsIcon
                    icon={Loading03Icon}
                    className="size-4 animate-spin"
                    strokeWidth={2}
                  />
                )}
                Save changes
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
