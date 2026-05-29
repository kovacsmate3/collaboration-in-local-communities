"use client"

import type { AdminTermsVersionDetail } from "@/lib/api/admin/terms"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"

interface TermsContentViewDialogProps {
  version: AdminTermsVersionDetail | null
  onOpenChange: (open: boolean) => void
}

export function TermsContentViewDialog({
  version,
  onOpenChange,
}: TermsContentViewDialogProps) {
  return (
    <Dialog open={Boolean(version)} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>
            {version?.title}{" "}
            <span className="font-mono text-sm font-normal text-muted-foreground">
              v{version?.version}
            </span>
          </DialogTitle>
        </DialogHeader>
        {version && (
          <div className="max-h-[70vh] overflow-y-auto">
            {version.content ? (
              <div
                className="prose prose-sm dark:prose-invert max-w-none"
                dangerouslySetInnerHTML={{ __html: version.content }}
              />
            ) : version.contentUrl ? (
              <p className="text-sm text-muted-foreground">
                Content hosted at:{" "}
                <a
                  href={version.contentUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="underline hover:no-underline"
                >
                  {version.contentUrl}
                </a>
              </p>
            ) : (
              <p className="text-sm text-muted-foreground">No content.</p>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
