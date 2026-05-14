"use client"

import { toast } from "sonner"
import { HugeiconsIcon } from "@hugeicons/react"
import { Loading03Icon } from "@hugeicons/core-free-icons"

import { ApiError } from "@/lib/api/client"
import {
  useDeleteCategory,
  type AdminCategoryResponse,
} from "@/lib/api/admin/categories"

import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"

interface DeleteCategoryDialogProps {
  category: AdminCategoryResponse | null
  onOpenChange: (open: boolean) => void
}

export function DeleteCategoryDialog({
  category,
  onOpenChange,
}: DeleteCategoryDialogProps) {
  const { mutateAsync, isPending } = useDeleteCategory()

  async function handleConfirm() {
    if (!category) return
    try {
      await mutateAsync(category.id)
      toast.success(`Category "${category.name}" deleted`)
      onOpenChange(false)
    } catch (err) {
      // Backend returns 409 with a ProblemDetails body when a task still
      // references the category. Surface the explanatory detail/title.
      if (err instanceof ApiError && err.status === 409) {
        const body = err.body as { title?: string; detail?: string } | undefined
        toast.error(
          body?.detail ??
            body?.title ??
            "Cannot delete: this category is still referenced by one or more tasks. Deactivate it instead."
        )
        return
      }
      toast.error(
        err instanceof Error ? err.message : "Failed to delete category"
      )
    }
  }

  return (
    <AlertDialog
      open={Boolean(category)}
      onOpenChange={(o) => {
        if (!isPending) onOpenChange(o)
      }}
    >
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete category permanently?</AlertDialogTitle>
          <AlertDialogDescription>
            This will permanently remove <strong>{category?.name}</strong>{" "}
            (code:{" "}
            <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">
              {category?.code}
            </code>
            ). This action cannot be undone. If any task still references this
            category the deletion will be blocked — deactivate it instead to
            hide it from the task creation flow without affecting existing
            tasks.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <Button
            variant="destructive-solid"
            onClick={handleConfirm}
            disabled={isPending}
          >
            {isPending && (
              <HugeiconsIcon
                icon={Loading03Icon}
                className="size-4 animate-spin"
                strokeWidth={2}
              />
            )}
            Delete permanently
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
