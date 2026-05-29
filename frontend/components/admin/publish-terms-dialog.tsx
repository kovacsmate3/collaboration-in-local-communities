"use client"

import type { ReactNode } from "react"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import {
  type AdminTermsVersionListItem,
  usePublishTermsVersion,
} from "@/lib/api/admin/terms"

interface PublishTermsDialogProps {
  version: AdminTermsVersionListItem | null
  onOpenChange: (open: boolean) => void
}

function getDialogCopy(version: AdminTermsVersionListItem | null): {
  title: string
  description: ReactNode
  action: string
} {
  if (!version) return { title: "", description: "", action: "Publish" }

  const versionLabel = `${version.majorVersion}.${version.minorVersion}.x`
  const isScheduled =
    version.publishedAt === null && new Date(version.effectiveFrom) > new Date()
  const isRepublish = version.publishedAt !== null

  if (isRepublish) {
    return {
      title: `Republish version ${version.version}?`,
      description: (
        <>
          This will replace the current active terms version with{" "}
          <strong>{version.version}</strong>. Users who have not accepted any{" "}
          <strong>{versionLabel}</strong> version will be prompted to re-accept.
        </>
      ),
      action: "Republish",
    }
  }

  if (isScheduled) {
    return {
      title: `Schedule version ${version.version}?`,
      description: (
        <>
          Version <strong>{version.version}</strong> will become active on{" "}
          <strong>
            {new Date(version.effectiveFrom).toLocaleString(undefined, {
              dateStyle: "medium",
              timeStyle: "short",
            })}
          </strong>
          . Any previously scheduled version will be cancelled. Users will be
          prompted to re-accept once it activates.
        </>
      ),
      action: "Schedule",
    }
  }

  return {
    title: `Publish version ${version.version}?`,
    description: (
      <>
        This will replace the current active terms version. Users who have not
        accepted any <strong>{versionLabel}</strong> version will be prompted to
        re-accept when they next open the app.
      </>
    ),
    action: "Publish",
  }
}

export function PublishTermsDialog({
  version,
  onOpenChange,
}: PublishTermsDialogProps) {
  const publish = usePublishTermsVersion()
  const copy = getDialogCopy(version)

  function handleConfirm() {
    if (!version) return
    publish.mutate(version.id, {
      onSuccess: () => onOpenChange(false),
    })
  }

  return (
    <AlertDialog open={Boolean(version)} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{copy.title}</AlertDialogTitle>
          <AlertDialogDescription asChild>
            <span>{copy.description}</span>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={publish.isPending}
          >
            {publish.isPending ? `${copy.action}…` : copy.action}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
