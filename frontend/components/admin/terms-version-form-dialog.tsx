"use client"

import * as React from "react"
import { marked } from "marked"
import TurndownService from "turndown"

import {
  type AdminTermsVersionDetail,
  type AdminTermsVersionListItem,
  useCreateTermsVersion,
  usePublishTermsVersion,
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
  allowedVersions: string[]
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
  existing?: AdminTermsVersionDetail | AdminTermsVersionListItem | null,
  defaultVersion?: string
): FormValues {
  if (!existing) {
    return {
      version: defaultVersion ?? "",
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
  defaultVersion,
  allowedVersions,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  defaultVersion?: string
  allowedVersions: string[]
}) {
  const initial = buildInitialValues(null, defaultVersion)
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Create terms version</DialogTitle>
        </DialogHeader>
        {open && (
          <TermsVersionFormBody
            key={`create-${defaultVersion}`}
            mode="create"
            initial={initial}
            allowedVersions={allowedVersions}
            onClose={() => onOpenChange(false)}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}

export function EditTermsVersionDialog({
  version,
  allowedVersions,
  onOpenChange,
}: {
  version: AdminTermsVersionDetail | null
  allowedVersions: string[]
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
            allowedVersions={allowedVersions}
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
  allowedVersions,
  onClose,
}: TermsVersionFormBodyProps) {
  const create = useCreateTermsVersion()
  const update = useUpdateTermsVersion(existingId ?? "")
  const publish = usePublishTermsVersion()

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

  function validate(requireContent = false): boolean {
    const errs: Partial<FormValues> = {}
    const trimmedVersion = values.version.trim()
    if (!trimmedVersion) {
      errs.version = "Required"
    } else if (!/^\d+\.\d+\.\d+$/.test(trimmedVersion)) {
      errs.version = "Must be x.y.z format (e.g. 0.1.0)"
    } else if (
      !(mode === "edit" && trimmedVersion === initial.version) &&
      !allowedVersions.includes(trimmedVersion)
    ) {
      errs.version = `Must be one of: ${allowedVersions.join(", ")}`
    }
    if (!values.title.trim()) errs.title = "Required"
    if (!values.effectiveFrom) errs.effectiveFrom = "Required"
    if (requireContent && !values.content.trim())
      errs.content = "Content is required before publishing"
    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  async function resolveContent(): Promise<string> {
    if (editorTab === "markdown") {
      return markdownToHtml(markdownSource)
    }
    return values.content
  }

  async function handleSubmit(e: React.SyntheticEvent) {
    e.preventDefault()
    if (!validate()) return

    const finalContent = await resolveContent()
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

  async function handlePublish(e: React.SyntheticEvent) {
    e.preventDefault()
    const finalContent = await resolveContent()
    setValues((v) => ({ ...v, content: finalContent }))

    if (!validate(true)) return

    const payload = {
      version: values.version.trim(),
      title: values.title.trim(),
      content: finalContent || undefined,
      contentUrl: values.contentUrl.trim() || undefined,
      effectiveFrom: datetimeLocalToIso(values.effectiveFrom),
    }

    if (mode === "edit" && existingId) {
      // Save changes first, then publish the existing draft.
      update.mutate(payload, {
        onSuccess: () => {
          publish.mutate(existingId, { onSuccess: onClose })
        },
      })
    } else {
      // Create first, then publish the resulting draft.
      create.mutate(payload, {
        onSuccess: (created) => {
          publish.mutate(created.id, { onSuccess: onClose })
        },
      })
    }
  }

  const isSavePending = create.isPending || update.isPending
  const isPublishPending = isSavePending || publish.isPending
  const serverError =
    create.error?.message ??
    update.error?.message ??
    publish.error?.message ??
    null

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
            {allowedVersions.length === 3
              ? `Allowed: ${allowedVersions[0]} (patch), ${allowedVersions[1]} (minor), ${allowedVersions[2]} (major). Patch bumps don't require re-acceptance.`
              : `Allowed: ${allowedVersions.join(", ")}.`}
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
        {errors.content && (
          <p className="text-xs text-destructive">{errors.content}</p>
        )}
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
        <Button
          type="button"
          variant="secondary"
          disabled={isPublishPending}
          onClick={(e) => void handlePublish(e)}
        >
          {isPublishPending ? "Publishing…" : "Publish"}
        </Button>
        <Button type="submit" disabled={isSavePending || publish.isPending}>
          {isSavePending ? "Saving…" : "Save as draft"}
        </Button>
      </DialogFooter>
    </form>
  )
}
