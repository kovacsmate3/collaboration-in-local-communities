"use client"

import {
  Delete02Icon,
  Edit01Icon,
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

interface TermsVersionsTableProps {
  versions: AdminTermsVersionListItem[]
  isLoading: boolean
  onPublish: (version: AdminTermsVersionListItem) => void
  onEdit: (id: string) => void
  onDelete: (version: AdminTermsVersionListItem) => void
}

export function TermsVersionsTable({
  versions,
  isLoading,
  onPublish,
  onEdit,
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
              onPublish={onPublish}
              onEdit={onEdit}
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
      <TableHead className="w-28">Status</TableHead>
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
  onPublish,
  onEdit,
  onDelete,
}: {
  version: AdminTermsVersionListItem
  onPublish: (v: AdminTermsVersionListItem) => void
  onEdit: (id: string) => void
  onDelete: (v: AdminTermsVersionListItem) => void
}) {
  const isDraft = !version.isActive

  return (
    <TableRow className={version.isActive ? "bg-success/5" : undefined}>
      <TableCell>
        <StatusBadge isActive={version.isActive} />
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
            {isDraft && (
              <>
                <DropdownMenuItem onClick={() => onPublish(version)}>
                  <HugeiconsIcon
                    icon={Upload01Icon}
                    className="mr-2 size-4"
                    strokeWidth={1.5}
                  />
                  Publish
                </DropdownMenuItem>
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
            {!isDraft && (
              <DropdownMenuItem disabled>
                Published — read-only
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </TableCell>
    </TableRow>
  )
}

function StatusBadge({ isActive }: { isActive: boolean }) {
  if (isActive) {
    return <Badge variant="success">Active</Badge>
  }
  return (
    <Badge variant="outline" className="text-muted-foreground">
      Draft
    </Badge>
  )
}
