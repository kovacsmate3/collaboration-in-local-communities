"use client"

import {
  Delete02Icon,
  Edit01Icon,
  EyeIcon,
  MoreVerticalIcon,
  Upload01Icon,
} from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import type { AdminTermsVersionListItem } from "@/lib/api/admin/terms"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"

type VersionStatus =
  | "active"
  | "scheduled"
  | "draft"
  | "old"
  | "old-republishable"

function getVersionStatus(
  v: AdminTermsVersionListItem,
  republishableId: string | null
): VersionStatus {
  if (v.isActive) return "active"
  if (!v.publishedAt) return "draft"
  if (new Date(v.effectiveFrom) > new Date()) return "scheduled"
  if (v.id === republishableId) return "old-republishable"
  return "old"
}

interface TermsVersionsTableProps {
  versions: AdminTermsVersionListItem[]
  isLoading: boolean
  republishableId: string | null
  onPublish: (version: AdminTermsVersionListItem) => void
  onEdit: (id: string) => void
  onView: (id: string) => void
  onDelete: (version: AdminTermsVersionListItem) => void
}

export function TermsVersionsTable({
  versions,
  isLoading,
  republishableId,
  onPublish,
  onEdit,
  onView,
  onDelete,
}: TermsVersionsTableProps) {
  if (isLoading) {
    return (
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TermsTableHeader />
          </TableHeader>
          <TableBody>
            {Array.from({ length: 3 }).map((_, i) => (
              <TableRow key={i}>
                <TableCell>
                  <div className="h-4 w-16 animate-pulse rounded bg-muted" />
                </TableCell>
                <TableCell>
                  <div className="h-4 w-32 animate-pulse rounded bg-muted" />
                </TableCell>
                <TableCell>
                  <div className="h-4 w-24 animate-pulse rounded bg-muted" />
                </TableCell>
                <TableCell>
                  <div className="h-4 w-10 animate-pulse rounded bg-muted" />
                </TableCell>
                <TableCell />
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    )
  }

  if (versions.length === 0) {
    return (
      <div className="rounded-md border px-6 py-12 text-center">
        <p className="text-sm text-muted-foreground">
          No terms versions yet. Create a draft to get started.
        </p>
      </div>
    )
  }

  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TermsTableHeader />
        </TableHeader>
        <TableBody>
          {versions.map((v) => (
            <TermsVersionRow
              key={v.id}
              version={v}
              status={getVersionStatus(v, republishableId)}
              onPublish={onPublish}
              onEdit={onEdit}
              onView={onView}
              onDelete={onDelete}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

function TermsTableHeader() {
  return (
    <TableRow>
      <TableHead className="w-36">Status</TableHead>
      <TableHead>Version</TableHead>
      <TableHead>Title</TableHead>
      <TableHead>Effective</TableHead>
      <TableHead className="text-right">Accepted by</TableHead>
      <TableHead className="w-10" />
    </TableRow>
  )
}

function TermsVersionRow({
  version,
  status,
  onPublish,
  onEdit,
  onView,
  onDelete,
}: {
  version: AdminTermsVersionListItem
  status: VersionStatus
  onPublish: (v: AdminTermsVersionListItem) => void
  onEdit: (id: string) => void
  onView: (id: string) => void
  onDelete: (v: AdminTermsVersionListItem) => void
}) {
  const rowBg =
    status === "active"
      ? "bg-success/5"
      : status === "scheduled"
        ? "bg-primary/5"
        : undefined

  return (
    <TableRow className={rowBg}>
      <TableCell>
        <StatusBadge status={status} />
      </TableCell>
      <TableCell className="font-mono text-sm">{version.version}</TableCell>
      <TableCell className="text-sm">{version.title}</TableCell>
      <TableCell className="text-sm text-muted-foreground">
        {new Date(version.effectiveFrom).toLocaleDateString()}
      </TableCell>
      <TableCell className="text-right text-sm tabular-nums">
        {version.acceptanceCount.toLocaleString()}
      </TableCell>
      <TableCell>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="size-8">
              <HugeiconsIcon
                icon={MoreVerticalIcon}
                className="size-4"
                strokeWidth={1.5}
              />
              <span className="sr-only">Actions</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {(status === "draft" ||
              status === "old-republishable" ||
              status === "scheduled") && (
              <DropdownMenuItem onClick={() => onPublish(version)}>
                <HugeiconsIcon
                  icon={Upload01Icon}
                  className="mr-2 size-4"
                  strokeWidth={1.5}
                />
                {status === "old-republishable" ? "Republish" : "Publish"}
              </DropdownMenuItem>
            )}
            {status === "draft" && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => onEdit(version.id)}>
                  <HugeiconsIcon
                    icon={Edit01Icon}
                    className="mr-2 size-4"
                    strokeWidth={1.5}
                  />
                  Edit
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  variant="destructive"
                  onClick={() => onDelete(version)}
                >
                  <HugeiconsIcon
                    icon={Delete02Icon}
                    className="mr-2 size-4"
                    strokeWidth={1.5}
                  />
                  Delete
                </DropdownMenuItem>
              </>
            )}
            {(status === "active" ||
              status === "old" ||
              status === "old-republishable" ||
              status === "scheduled") && (
              <>
                {(status === "old-republishable" || status === "scheduled") && (
                  <DropdownMenuSeparator />
                )}
                <DropdownMenuItem onClick={() => onView(version.id)}>
                  <HugeiconsIcon
                    icon={EyeIcon}
                    className="mr-2 size-4"
                    strokeWidth={1.5}
                  />
                  View content
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </TableCell>
    </TableRow>
  )
}

function StatusBadge({ status }: { status: VersionStatus }) {
  switch (status) {
    case "active":
      return <Badge variant="success">Active</Badge>
    case "scheduled":
      return <Badge variant="secondary">Scheduled</Badge>
    case "draft":
      return (
        <Badge variant="outline" className="text-muted-foreground">
          Draft
        </Badge>
      )
    case "old-republishable":
      return <Badge variant="warning">Old · Republishable</Badge>
    case "old":
      return (
        <Badge variant="outline" className="text-muted-foreground/60">
          Old
        </Badge>
      )
  }
}
