"use client"

import { toast } from "sonner"
import { HugeiconsIcon } from "@hugeicons/react"
import { Loading03Icon } from "@hugeicons/core-free-icons"

import { useDeleteCategory, type AdminCategoryResponse } from "@/lib/api/admin/categories"

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
      toast.success(`Category "${category.name}" deactivated`)
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Failed to deactivate category",
      )
    }
  }

  return (
    <AlertDialog open={Boolean(category)} onOpenChange={(o) => { if (!isPending) onOpenChange(o) }}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Deactivate category?</AlertDialogTitle>
          <AlertDialogDescription>
            This will soft-delete <strong>{category?.name}</strong> (code:{" "}
            <code className="rounded bg-muted px-1 py-0.5 text-xs font-mono">
              {category?.code}
            </code>
            ). Existing tasks that reference it will be unaffected, but the
            category will no longer appear in the task creation flow. You can
            reactivate it later by editing its status directly in the database
            until a reactivation UI is added.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <Button
            variant="destructive"
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
            Deactivate
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
