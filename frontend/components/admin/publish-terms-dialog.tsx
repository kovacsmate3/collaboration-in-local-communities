"use client"

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

export function PublishTermsDialog({
  version,
  onOpenChange,
}: PublishTermsDialogProps) {
  const publish = usePublishTermsVersion()

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
          <AlertDialogTitle>
            Publish version {version?.version}?
          </AlertDialogTitle>
          <AlertDialogDescription>
            This will replace the current active terms version. Users who have
            not accepted any{" "}
            <strong>
              {version?.majorVersion}.{version?.minorVersion}.x
            </strong>{" "}
            version will be prompted to re-accept when they next open the app.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={publish.isPending}
          >
            {publish.isPending ? "Publishing…" : "Publish"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
