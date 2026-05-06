"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { toast } from "sonner"

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
import { Badge } from "@/components/ui/badge"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Form } from "@/components/ui/form"
import {
  CategoryFormActions,
  CategoryFormFields,
} from "@/components/admin/category-form-fields"

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
      onOpenChange={(nextOpen) => {
        if (!isPending) {
          onOpenChange(nextOpen)
        }
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
            <CategoryFormFields
              includeCode
              namePlaceholder="e.g. Garden Work"
              descriptionPlaceholder="Brief description for seekers..."
            />
            <CategoryFormActions
              isPending={isPending}
              onCancel={() => onOpenChange(false)}
              submitLabel="Create category"
            />
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}

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
    if (!category) {
      return
    }

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
      onOpenChange={(nextOpen) => {
        if (!isPending) {
          onOpenChange(nextOpen)
        }
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
            <CategoryFormFields />
            <CategoryFormActions
              isPending={isPending}
              onCancel={() => onOpenChange(false)}
              submitLabel="Save changes"
            />
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
