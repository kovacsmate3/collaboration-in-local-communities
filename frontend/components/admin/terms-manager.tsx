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
import { TermsContentViewDialog } from "@/components/admin/terms-content-view-dialog"
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

function computeAllowedNextVersions(
  versions: AdminTermsVersionListItem[],
  excludeId?: string
): string[] {
  const others = excludeId
    ? versions.filter((v) => v.id !== excludeId)
    : versions
  if (others.length === 0) return ["0.1.0"]

  const highest = others.reduce<{
    major: number
    minor: number
    patch: number
  } | null>((acc, v) => {
    if (!acc)
      return {
        major: v.majorVersion,
        minor: v.minorVersion,
        patch: v.patchVersion,
      }
    if (
      v.majorVersion > acc.major ||
      (v.majorVersion === acc.major && v.minorVersion > acc.minor) ||
      (v.majorVersion === acc.major &&
        v.minorVersion === acc.minor &&
        v.patchVersion > acc.patch)
    ) {
      return {
        major: v.majorVersion,
        minor: v.minorVersion,
        patch: v.patchVersion,
      }
    }
    return acc
  }, null)

  if (!highest) return ["0.1.0"]
  return [
    `${highest.major}.${highest.minor}.${highest.patch + 1}`,
    `${highest.major}.${highest.minor + 1}.0`,
    `${highest.major + 1}.0.0`,
  ]
}

export function TermsManager() {
  const { data, isLoading, isError, error, refetch } = useAdminTermsList()

  const [createOpen, setCreateOpen] = React.useState(false)
  const [publishTarget, setPublishTarget] =
    React.useState<AdminTermsVersionListItem | null>(null)
  const [editTargetId, setEditTargetId] = React.useState<string | null>(null)
  const [viewTargetId, setViewTargetId] = React.useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] =
    React.useState<AdminTermsVersionListItem | null>(null)

  const { data: editDetail } = useAdminTermsById(editTargetId ?? "")
  const { data: viewDetail } = useAdminTermsById(viewTargetId ?? "")
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

  const republishableId = React.useMemo(() => {
    if (!data) return null
    const now = new Date()
    const oldVersions = data.filter(
      (v) =>
        !v.isActive &&
        v.publishedAt !== null &&
        new Date(v.effectiveFrom) <= now
    )
    if (oldVersions.length === 0) return null
    return oldVersions.sort(
      (a, b) =>
        new Date(b.publishedAt as string).getTime() -
        new Date(a.publishedAt as string).getTime()
    )[0].id
  }, [data])

  const allowedNextForCreate = React.useMemo(
    () => computeAllowedNextVersions(data ?? []),
    [data]
  )

  const allowedNextForEdit = React.useMemo(
    () =>
      editTargetId ? computeAllowedNextVersions(data ?? [], editTargetId) : [],
    [data, editTargetId]
  )

  const nextDefaultVersion = allowedNextForCreate[0] ?? ""

  const histogramData: AdminBarChartRow[] = React.useMemo(() => {
    if (!data || data.length === 0) return []
    // data is sorted effectiveFrom DESC — iterate in that order so the first
    // 5 unique major.minor groups are the 5 most recent.
    const groups = new Map<string, { label: string; value: number }>()
    for (const v of data) {
      const key = `${v.majorVersion}.${v.minorVersion}`
      const existing = groups.get(key)
      if (existing) {
        existing.value += v.acceptanceCount
      } else {
        if (groups.size >= 5) continue
        groups.set(key, {
          label: `${v.majorVersion}.${v.minorVersion}.x`,
          value: v.acceptanceCount,
        })
      }
    }
    // Reverse so the chart reads oldest → newest (left to right).
    const rows = Array.from(groups.values()).reverse()
    const max = Math.max(...rows.map((r) => r.value), 1)
    return rows.map((r) => ({
      label: r.label,
      value: r.value,
      pct: Math.round((r.value / max) * 100),
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
        republishableId={republishableId}
        onPublish={setPublishTarget}
        onEdit={(id) => handleEdit(id)}
        onView={(id) => setViewTargetId(id)}
        onDelete={setDeleteTarget}
      />

      <TermsContentViewDialog
        version={viewDetail ?? null}
        onOpenChange={(open) => {
          if (!open) setViewTargetId(null)
        }}
      />

      <CreateTermsVersionDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        defaultVersion={nextDefaultVersion}
        allowedVersions={allowedNextForCreate}
      />

      <EditTermsVersionDialog
        version={editDetail ?? null}
        allowedVersions={allowedNextForEdit}
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
