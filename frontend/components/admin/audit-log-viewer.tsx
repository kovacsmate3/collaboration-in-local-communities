"use client"

import { useState, useEffect, useRef } from "react"
import {
  type AuditLogEntry,
  type AuditLogParams,
  useAuditLog,
} from "@/lib/api/admin/audit-log"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { formatDatetime } from "@/lib/format"

function shortId(id: string) {
  return `${id.slice(0, 8)}…`
}

function PayloadCell({ payload }: { payload: string }) {
  let formatted = payload
  try {
    formatted = JSON.stringify(JSON.parse(payload), null, 2)
  } catch {
    // leave as-is if not valid JSON
  }

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="h-6 px-2 text-xs">
          View
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Event Payload</DialogTitle>
        </DialogHeader>
        <pre className="max-h-96 overflow-auto rounded-md bg-muted p-3 text-xs">
          {formatted}
        </pre>
      </DialogContent>
    </Dialog>
  )
}

function TableSkeleton() {
  return (
    <>
      {Array.from({ length: 8 }).map((_, i) => (
        <TableRow key={i}>
          <TableCell>
            <Skeleton className="h-4 w-36" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-4 w-40" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-4 w-32" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-4 w-28" />
          </TableCell>
          <TableCell>
            <Skeleton className="h-4 w-10" />
          </TableCell>
        </TableRow>
      ))}
    </>
  )
}

function AuditLogRow({ entry }: { entry: AuditLogEntry }) {
  return (
    <TableRow>
      <TableCell className="text-xs whitespace-nowrap text-muted-foreground">
        {formatDatetime(entry.createdAt)}
      </TableCell>
      <TableCell>
        <span className="rounded bg-muted px-1.5 py-0.5 font-mono text-xs">
          {entry.eventType}
        </span>
      </TableCell>
      <TableCell>
        {entry.actorUserId ? (
          <div className="flex flex-col gap-0.5">
            <span className="text-sm">
              {entry.actorDisplayName ?? entry.actorEmail ?? "Unknown"}
            </span>
            {entry.actorDisplayName && entry.actorEmail && (
              <span className="text-xs text-muted-foreground">
                {entry.actorEmail}
              </span>
            )}
          </div>
        ) : (
          <span className="text-sm text-muted-foreground">System</span>
        )}
      </TableCell>
      <TableCell>
        {entry.entityType ? (
          <div className="flex flex-col gap-0.5">
            <span className="text-sm">{entry.entityType}</span>
            {entry.entityId && (
              <span
                className="font-mono text-xs text-muted-foreground"
                title={entry.entityId}
              >
                {shortId(entry.entityId)}
              </span>
            )}
          </div>
        ) : (
          <span className="text-muted-foreground">&mdash;</span>
        )}
      </TableCell>
      <TableCell>
        {entry.payload ? <PayloadCell payload={entry.payload} /> : null}
      </TableCell>
    </TableRow>
  )
}

const GUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

function isValidGuid(value: string) {
  return GUID_RE.test(value)
}

export function AuditLogViewer() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState("")
  const [debouncedSearch, setDebouncedSearch] = useState("")
  const [entityType, setEntityType] = useState("")
  const [actorUserId, setActorUserId] = useState("")
  const [from, setFrom] = useState("")
  const [to, setTo] = useState("")

  const actorUserIdError =
    actorUserId && !isValidGuid(actorUserId) ? "Must be a valid UUID" : null

  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 300)
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current)
    }
  }, [search])

  const params: AuditLogParams = {
    page,
    pageSize: 20,
    ...(debouncedSearch ? { search: debouncedSearch } : {}),
    ...(entityType ? { entityType } : {}),
    ...(actorUserId && isValidGuid(actorUserId) ? { actorUserId } : {}),
    ...(from ? { from } : {}),
    ...(to ? { to: `${to}T23:59:59.999Z` } : {}),
  }

  const { data, isLoading, isError } = useAuditLog(params)

  const hasFilters = Boolean(
    debouncedSearch || entityType || actorUserId || from || to
  )

  function clearFilters() {
    setSearch("")
    setDebouncedSearch("")
    setEntityType("")
    setActorUserId("")
    setFrom("")
    setTo("")
    setPage(1)
  }

  function handleFilterChange(setter: (v: string) => void) {
    return (e: React.ChangeEvent<HTMLInputElement>) => {
      setter(e.target.value)
      setPage(1)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-2">
        <Input
          placeholder="Search event type…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-48"
        />
        <Input
          placeholder="Entity type…"
          value={entityType}
          onChange={handleFilterChange(setEntityType)}
          className="w-40"
        />
        <div className="flex flex-col gap-1">
          <Input
            placeholder="Actor user ID…"
            value={actorUserId}
            onChange={handleFilterChange(setActorUserId)}
            className={actorUserIdError ? "w-72 border-destructive" : "w-72"}
            aria-describedby={actorUserIdError ? "actor-id-error" : undefined}
          />
          {actorUserIdError && (
            <p id="actor-id-error" className="text-xs text-destructive">
              {actorUserIdError}
            </p>
          )}
        </div>
        <div className="flex items-center gap-1">
          <span className="text-xs text-muted-foreground">From</span>
          <Input
            type="date"
            value={from}
            onChange={handleFilterChange(setFrom)}
            className="w-40"
          />
        </div>
        <div className="flex items-center gap-1">
          <span className="text-xs text-muted-foreground">To</span>
          <Input
            type="date"
            value={to}
            onChange={handleFilterChange(setTo)}
            className="w-40"
          />
        </div>
        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            Clear filters
          </Button>
        )}
      </div>

      {isError && (
        <Alert variant="destructive">
          <AlertTitle>Failed to load audit log</AlertTitle>
          <AlertDescription>
            There was a problem fetching the audit events. Please try again.
          </AlertDescription>
        </Alert>
      )}

      <div className="rounded-lg border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Time</TableHead>
              <TableHead>Event</TableHead>
              <TableHead>Actor</TableHead>
              <TableHead>Entity</TableHead>
              <TableHead>Payload</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeleton />
            ) : data?.items.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="py-10 text-center text-sm text-muted-foreground"
                >
                  No audit events found.
                </TableCell>
              </TableRow>
            ) : (
              data?.items.map((entry) => (
                <AuditLogRow key={entry.id} entry={entry} />
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {data && data.totalPages > 0 && (
        <div className="flex items-center justify-between">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
          >
            &larr; Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {data.page} of {data.totalPages} ({data.totalCount}{" "}
            {data.totalCount === 1 ? "entry" : "entries"})
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
            disabled={page >= data.totalPages}
          >
            Next &rarr;
          </Button>
        </div>
      )}
    </div>
  )
}
