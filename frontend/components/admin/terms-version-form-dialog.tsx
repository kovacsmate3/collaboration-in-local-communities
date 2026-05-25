"use client"

import * as React from "react"
import { marked } from "marked"
import TurndownService from "turndown"

import {
  type AdminTermsVersionDetail,
  type AdminTermsVersionListItem,
  useCreateTermsVersion,
  useUpdateTermsVersion,
} from "@/lib/api/admin/terms"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { RichTextEditor } from "@/components/shared/rich-text-editor"

const turndown = new TurndownService({
  headingStyle: "atx",
  bulletListMarker: "-",
})

interface FormValues {
  version: string
  title: string
  content: string
  contentUrl: string
  effectiveFrom: string
}

interface TermsVersionFormBodyProps {
  mode: "create" | "edit"
  initial: FormValues
  existingId?: string
  onClose: () => void
}

function htmlToMarkdown(html: string): string {
  try {
    return turndown.turndown(html)
  } catch {
    return html
  }
}

async function markdownToHtml(md: string): Promise<string> {
  try {
    const result = await marked(md, { async: true })
    return typeof result === "string" ? result : md
  } catch {
    return md
  }
}

function isoToDatetimeLocal(iso: string): string {
  try {
    const d = new Date(iso)
    const pad = (n: number) => String(n).padStart(2, "0")
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
  } catch {
    return ""
  }
}

function datetimeLocalToIso(local: string): string {
  try {
    return new Date(local).toISOString()
  } catch {
    return new Date().toISOString()
  }
}

function buildInitialValues(
  existing?: AdminTermsVersionDetail | AdminTermsVersionListItem | null
): FormValues {
  if (!existing) {
    return {
      version: "",
      title: "",
      content: "",
      contentUrl: "",
      effectiveFrom: isoToDatetimeLocal(new Date().toISOString()),
    }
  }
  return {
    version: existing.version,
    title: existing.title,
    content: "content" in existing ? (existing.content ?? "") : "",
    contentUrl: "contentUrl" in existing ? (existing.contentUrl ?? "") : "",
    effectiveFrom: isoToDatetimeLocal(existing.effectiveFrom),
  }
}

// ── Public wrapper components ─────────────────────────────────────────────────

export function CreateTermsVersionDialog({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const initial = buildInitialValues(null)
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Create terms version</DialogTitle>
        </DialogHeader>
        {open && (
          <TermsVersionFormBody
            key="create"
            mode="create"
            initial={initial}
            onClose={() => onOpenChange(false)}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

export function EditTermsVersionDialog({
  version,
  onOpenChange,
}: {
  version: AdminTermsVersionDetail | null
  onOpenChange: (open: boolean) => void
}) {
  const initial = buildInitialValues(version)
  return (
    <Dialog open={Boolean(version)} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Edit terms version</DialogTitle>
        </DialogHeader>
        {Boolean(version) && (
          <TermsVersionFormBody
            key={version?.id}
            mode="edit"
            initial={initial}
            existingId={version?.id}
            onClose={() => onOpenChange(false)}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

// ── Form body (keyed so state resets on each open) ────────────────────────────

function TermsVersionFormBody({
  mode,
  initial,
  existingId,
  onClose,
}: TermsVersionFormBodyProps) {
  const create = useCreateTermsVersion()
  const update = useUpdateTermsVersion(existingId ?? "")

  const [values, setValues] = React.useState<FormValues>(initial)
  const [editorTab, setEditorTab] = React.useState<"visual" | "markdown">(
    "visual"
  )
  const [markdownSource, setMarkdownSource] = React.useState(() =>
    htmlToMarkdown(initial.content)
  )
  const [errors, setErrors] = React.useState<Partial<FormValues>>({})

  async function handleTabChange(tab: string) {
    if (tab === "markdown" && editorTab === "visual") {
      setMarkdownSource(htmlToMarkdown(values.content))
    } else if (tab === "visual" && editorTab === "markdown") {
      const html = await markdownToHtml(markdownSource)
      setValues((v) => ({ ...v, content: html }))
    }
    setEditorTab(tab as "visual" | "markdown")
  }

  function validate(): boolean {
    const errs: Partial<FormValues> = {}
    if (!values.version.trim()) errs.version = "Required"
    else if (!/^\d+\.\d+\.\d+$/.test(values.version.trim()))
      errs.version = "Must be x.y.z format (e.g. 0.1.0)"
    if (!values.title.trim()) errs.title = "Required"
    if (!values.effectiveFrom) errs.effectiveFrom = "Required"
    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    let finalContent = values.content
    if (editorTab === "markdown") {
      finalContent = await markdownToHtml(markdownSource)
    }

    if (!validate()) return

    const payload = {
      version: values.version.trim(),
      title: values.title.trim(),
      content: finalContent || undefined,
      contentUrl: values.contentUrl.trim() || undefined,
      effectiveFrom: datetimeLocalToIso(values.effectiveFrom),
    }

    if (mode === "create") {
      create.mutate(payload, { onSuccess: onClose })
    } else {
      update.mutate(payload, { onSuccess: onClose })
    }
  }

  const isPending = create.isPending || update.isPending
  const serverError = create.error?.message ?? update.error?.message ?? null

  return (
    <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="tv-version">Version</Label>
          <Input
            id="tv-version"
            placeholder="0.1.0"
            value={values.version}
            onChange={(e) =>
              setValues((v) => ({ ...v, version: e.target.value }))
            }
          />
          {errors.version && (
            <p className="text-xs text-destructive">{errors.version}</p>
          )}
          <p className="text-xs text-muted-foreground">
            Patch bumps (x.y.0 → x.y.1) won&apos;t require user re-acceptance.
          </p>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="tv-effective">Effective from</Label>
          <Input
            id="tv-effective"
            type="datetime-local"
            value={values.effectiveFrom}
            onChange={(e) =>
              setValues((v) => ({ ...v, effectiveFrom: e.target.value }))
            }
          />
          {errors.effectiveFrom && (
            <p className="text-xs text-destructive">{errors.effectiveFrom}</p>
          )}
        </div>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="tv-title">Title</Label>
        <Input
          id="tv-title"
          placeholder="Terms and Conditions"
          value={values.title}
          onChange={(e) => setValues((v) => ({ ...v, title: e.target.value }))}
        />
        {errors.title && (
          <p className="text-xs text-destructive">{errors.title}</p>
        )}
      </div>

      <div className="space-y-1.5">
        <Label>Content</Label>
        <Tabs value={editorTab} onValueChange={(t) => void handleTabChange(t)}>
          <TabsList className="mb-2 h-8">
            <TabsTrigger value="visual" className="text-xs">
              Visual
            </TabsTrigger>
            <TabsTrigger value="markdown" className="text-xs">
              Markdown
            </TabsTrigger>
          </TabsList>
          <TabsContent value="visual" className="mt-0">
            <RichTextEditor
              value={values.content}
              onChange={(html) => setValues((v) => ({ ...v, content: html }))}
              placeholder="Write the terms content…"
              className="min-h-64"
            />
          </TabsContent>
          <TabsContent value="markdown" className="mt-0">
            <textarea
              className="w-full rounded-md border border-input bg-background p-3 font-mono text-sm placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-ring focus-visible:outline-none"
              rows={16}
              placeholder={"## 1. About\n\nYour terms content here…"}
              value={markdownSource}
              onChange={(e) => setMarkdownSource(e.target.value)}
            />
          </TabsContent>
        </Tabs>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="tv-url">Content URL (optional)</Label>
        <Input
          id="tv-url"
          type="url"
          placeholder="https://example.com/terms.pdf"
          value={values.contentUrl}
          onChange={(e) =>
            setValues((v) => ({ ...v, contentUrl: e.target.value }))
          }
        />
      </div>

      {serverError && <p className="text-sm text-destructive">{serverError}</p>}

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending
            ? mode === "create"
              ? "Creating…"
              : "Saving…"
            : mode === "create"
              ? "Create draft"
              : "Save changes"}
        </Button>
      </DialogFooter>
    </form>
  )
}
