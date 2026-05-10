"use client"

import type { CSSProperties } from "react"
import { toast } from "sonner"
import { HugeiconsIcon } from "@hugeicons/react"
import { Loading03Icon } from "@hugeicons/core-free-icons"

import {
  useActivateCategory,
  useDeactivateCategory,
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

interface CategoryStatusDialogProps {
  category: AdminCategoryResponse | null
  onOpenChange: (open: boolean) => void
}

/**
 * Confirms an activate or deactivate action against a category. Renders as a
 * centered AlertDialog (same as the delete confirmation). The default shadcn
 * AlertDialog enters with a subtle slide from the upper-left in addition to
 * its zoom + fade; we zero out the slide so the dialog grows out from the
 * center (95% → 100%) with a fade-in, and reverses on close — no directional
 * sweep at all.
 *
 * The same component is reused for both flows; the prompt and copy are
 * derived from the target category's current `isActive` state.
 *
 * Deactivate: hides the category from new task creation; existing tasks are
 * unaffected.
 * Activate:   makes the category available again in the task creation flow.
 */
export function CategoryStatusDialog({
  category,
  onOpenChange,
}: CategoryStatusDialogProps) {
  const deactivate = useDeactivateCategory()
  const activate = useActivateCategory()

  // The dialog is opened with a snapshot of the row, so pick the action by
  // looking at the snapshot's current state.
  const willActivate = category?.isActive === false
  const mutation = willActivate ? activate : deactivate
  const isPending = mutation.isPending

  async function handleConfirm() {
    if (!category) return
    try {
      await mutation.mutateAsync(category.id)
      toast.success(
        willActivate
          ? `Category "${category.name}" reactivated`
          : `Category "${category.name}" deactivated`
      )
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : willActivate
            ? "Failed to reactivate category"
            : "Failed to deactivate category"
      )
    }
  }

  // The shadcn AlertDialog stacks four animated effects via tw-animate-css:
  // fade-in (`--tw-enter-opacity`), zoom-in-95 (`--tw-enter-scale`), plus a
  // slide on `--tw-enter-translate-x` (from `slide-in-from-left-1/2`) and
  // `--tw-enter-translate-y` (from `slide-in-from-top-[48%]`). Pinning both
  // translate variables to `0` (and the same on exit) cancels just the slide
  // — fade + zoom keep the dialog blooming out from the center on open and
  // shrinking back into it on close. Inline styles outrank utility classes,
  // so we don't need to remove the slide-in-from-* classes themselves.
  const zoomFromCenterStyle = {
    "--tw-enter-translate-x": "0",
    "--tw-enter-translate-y": "0",
    "--tw-exit-translate-x": "0",
    "--tw-exit-translate-y": "0",
  } as CSSProperties

  return (
    <AlertDialog
      open={Boolean(category)}
      onOpenChange={(o) => {
        if (!isPending) onOpenChange(o)
      }}
    >
      <AlertDialogContent style={zoomFromCenterStyle}>
        <AlertDialogHeader>
          <AlertDialogTitle>
            {willActivate ? "Reactivate category?" : "Deactivate category?"}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {willActivate ? (
              <>
                This will make <strong>{category?.name}</strong> (code:{" "}
                <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">
                  {category?.code}
                </code>
                ) available again in the task creation flow.
              </>
            ) : (
              <>
                This will hide <strong>{category?.name}</strong> (code:{" "}
                <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">
                  {category?.code}
                </code>
                ) from the task creation flow. Existing tasks that reference
                it will be unaffected, and you can reactivate it any time.
              </>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <Button
            variant={willActivate ? "default" : "destructive"}
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
            {willActivate ? "Reactivate" : "Deactivate"}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
