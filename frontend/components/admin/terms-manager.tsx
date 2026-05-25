"use client"

import * as React from "react"
import { Add01Icon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import {
  type AdminTermsVersionListItem,
  useAdminTermsList,
  useAdminTermsById,
  useDeleteTermsVersion,
} from "@/lib/api/admin/terms"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import {
  AdminBarChart,
  type AdminBarChartRow,
} from "@/components/admin/admin-bar-chart"
import { PublishTermsDialog } from "@/components/admin/publish-terms-dialog"
import { TermsVersionsTable } from "@/components/admin/terms-versions-table"
import {
  CreateTermsVersionDialog,
  EditTermsVersionDialog,
} from "@/components/admin/terms-version-form-dialog"
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

export function TermsManager() {
  const { data, isLoading, isError, error, refetch } = useAdminTermsList()

  const [createOpen, setCreateOpen] = React.useState(false)
  const [publishTarget, setPublishTarget] =
    React.useState<AdminTermsVersionListItem | null>(null)
  const [editTargetId, setEditTargetId] = React.useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] =
    React.useState<AdminTermsVersionListItem | null>(null)

  const { data: editDetail } = useAdminTermsById(editTargetId ?? "")
  const deleteVersion = useDeleteTermsVersion()

  function handleEdit(id: string) {
    setEditTargetId(id)
  }

  function handleDeleteConfirm() {
    if (!deleteTarget) return
    deleteVersion.mutate(deleteTarget.id, {
      onSuccess: () => setDeleteTarget(null),
    })
  }

  const histogramData: AdminBarChartRow[] = React.useMemo(() => {
    if (!data || data.length === 0) return []
    const max = Math.max(...data.map((v) => v.acceptanceCount), 1)
    return data.map((v) => ({
      label: v.version,
      value: v.acceptanceCount,
      pct: Math.round((v.acceptanceCount / max) * 100),
    }))
  }, [data])

  const totalAcceptances = React.useMemo(
    () => data?.reduce((sum, v) => sum + v.acceptanceCount, 0) ?? 0,
    [data]
  )

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">
            Terms Management
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Manage terms versions and track user acceptance
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="shrink-0">
          <HugeiconsIcon icon={Add01Icon} className="size-4" strokeWidth={2} />
          New version
        </Button>
      </div>

      {isError && (
        <Alert variant="destructive">
          <AlertDescription>
            Failed to load terms —{" "}
            {error instanceof Error ? error.message : "unknown error"}.{" "}
            <button
              className="underline hover:no-underline"
              onClick={() => void refetch()}
            >
              Retry
            </button>
          </AlertDescription>
        </Alert>
      )}

      {!isLoading && data && data.length > 0 && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium">
              Acceptances by version
              <span className="ml-2 font-normal text-muted-foreground">
                ({totalAcceptances.toLocaleString()} total)
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <AdminBarChart data={histogramData} colorClass="bg-primary" />
          </CardContent>
        </Card>
      )}

      <TermsVersionsTable
        versions={data ?? []}
        isLoading={isLoading}
        onPublish={setPublishTarget}
        onEdit={(id) => handleEdit(id)}
        onDelete={setDeleteTarget}
      />

      <CreateTermsVersionDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
      />

      <EditTermsVersionDialog
        version={editDetail ?? null}
        onOpenChange={(open) => {
          if (!open) setEditTargetId(null)
        }}
      />

      <PublishTermsDialog
        version={publishTarget}
        onOpenChange={(open) => {
          if (!open) setPublishTarget(null)
        }}
      />

      <AlertDialog
        open={Boolean(deleteTarget)}
        onOpenChange={(open) => {
          if (!open) setDeleteTarget(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              Delete version {deleteTarget?.version}?
            </AlertDialogTitle>
            <AlertDialogDescription>
              This draft will be permanently deleted. This action cannot be
              undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              disabled={deleteVersion.isPending}
              className="text-destructive-foreground bg-destructive hover:bg-destructive/90"
            >
              {deleteVersion.isPending ? "Deleting…" : "Delete"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
