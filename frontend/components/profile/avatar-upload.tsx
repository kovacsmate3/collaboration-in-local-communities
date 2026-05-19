"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import { Cancel01Icon, Edit01Icon, Loading03Icon } from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { UserAvatar } from "@/components/shared/user-avatar"
import { cn } from "@/lib/utils"
import {
  useDeleteProfilePhoto,
  useUploadProfilePhoto,
} from "@/lib/api/profile"

interface AvatarUploadProps {
  name: string
  currentPhotoUrl?: string | null
}

const ACCEPTED_TYPES = "image/jpeg,image/png,image/webp"
const MAX_SIZE_BYTES = 5 * 1024 * 1024

export function AvatarUpload({ name, currentPhotoUrl }: AvatarUploadProps) {
  const inputRef = React.useRef<HTMLInputElement>(null)
  const [preview, setPreview] = React.useState<string | null>(null)

  const uploadPhoto = useUploadProfilePhoto()
  const deletePhoto = useDeleteProfilePhoto()

  const isPending = uploadPhoto.isPending || deletePhoto.isPending
  const displaySrc = preview ?? currentPhotoUrl ?? undefined

  React.useEffect(() => {
    return () => {
      if (preview) URL.revokeObjectURL(preview)
    }
  }, [preview])

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return

    if (file.size > MAX_SIZE_BYTES) {
      toast.error("Photo must be 5 MB or smaller.")
      return
    }

    if (preview) URL.revokeObjectURL(preview)
    setPreview(URL.createObjectURL(file))

    uploadPhoto.mutate(file, {
      onError: (err) => {
        setPreview(null)
        toast.error(err instanceof Error ? err.message : "Failed to upload photo.")
      },
    })

    // Reset so the same file can be re-selected if needed
    e.target.value = ""
  }

  function handleDelete() {
    deletePhoto.mutate(undefined, {
      onSuccess: () => {
        if (preview) URL.revokeObjectURL(preview)
        setPreview(null)
      },
      onError: (err) => {
        toast.error(err instanceof Error ? err.message : "Failed to remove photo.")
      },
    })
  }

  const hasPhoto = Boolean(displaySrc)

  return (
    <div className="flex items-center gap-3">
      <div className="relative">
        <UserAvatar name={name} src={displaySrc} size="lg" />

        {isPending && (
          <div className="absolute inset-0 flex items-center justify-center rounded-full bg-background/70">
            <HugeiconsIcon
              icon={Loading03Icon}
              className="size-5 animate-spin text-foreground"
            />
          </div>
        )}

        {!isPending && (
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            className={cn(
              "absolute inset-0 flex items-center justify-center rounded-full",
              "bg-black/0 opacity-0 transition-opacity hover:bg-black/40 hover:opacity-100",
              "focus-visible:bg-black/40 focus-visible:opacity-100 focus-visible:outline-none"
            )}
            aria-label="Change profile photo"
          >
            <HugeiconsIcon
              icon={Edit01Icon}
              className="size-5 text-white"
            />
          </button>
        )}
      </div>

      <div className="flex flex-col gap-1.5">
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          disabled={isPending}
          className="text-sm font-medium underline-offset-4 hover:underline disabled:opacity-50"
        >
          {hasPhoto ? "Change photo" : "Upload photo"}
        </button>

        {hasPhoto && (
          <button
            type="button"
            onClick={handleDelete}
            disabled={isPending}
            className="flex items-center gap-1 text-sm text-muted-foreground underline-offset-4 hover:text-destructive hover:underline disabled:opacity-50"
          >
            <HugeiconsIcon icon={Cancel01Icon} className="size-3.5" />
            Remove
          </button>
        )}

        <p className="text-xs text-muted-foreground">
          JPEG, PNG or WebP · max 5 MB
        </p>
      </div>

      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED_TYPES}
        className="sr-only"
        onChange={handleFileChange}
        aria-hidden="true"
        tabIndex={-1}
      />
    </div>
  )
}
