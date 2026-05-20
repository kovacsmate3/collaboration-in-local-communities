"use client"

import * as React from "react"
import Cropper from "react-easy-crop"
import type { Area } from "react-easy-crop"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Cancel01Icon,
  Edit01Icon,
  Loading03Icon,
} from "@hugeicons/core-free-icons"
import { toast } from "sonner"

import { UserAvatar } from "@/components/shared/user-avatar"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Slider } from "@/components/ui/slider"
import { cn } from "@/lib/utils"
import { useAuth } from "@/lib/auth-context"
import { useDeleteProfilePhoto, useUploadProfilePhoto } from "@/lib/api/profile"

interface AvatarUploadProps {
  name: string
  currentPhotoUrl?: string | null
}

const ACCEPTED_MIME_TYPES = new Set(["image/jpeg", "image/png", "image/webp"])
const ACCEPTED_TYPES = "image/jpeg,image/png,image/webp"
const MAX_COMPRESSED_BYTES = 500 * 1024
const MAX_OUTPUT_PX = 800

function createImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.addEventListener("load", () => resolve(img))
    img.addEventListener("error", reject)
    img.src = src
  })
}

function canvasToBlob(
  canvas: HTMLCanvasElement,
  type: string,
  quality: number
): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (b) => (b ? resolve(b) : reject(new Error("Canvas toBlob failed"))),
      type,
      quality
    )
  })
}

async function getCroppedBlob(
  imageSrc: string,
  pixelCrop: Area
): Promise<Blob> {
  const image = await createImage(imageSrc)
  const scale = Math.min(
    1,
    MAX_OUTPUT_PX / pixelCrop.width,
    MAX_OUTPUT_PX / pixelCrop.height
  )
  const outW = Math.round(pixelCrop.width * scale)
  const outH = Math.round(pixelCrop.height * scale)

  const canvas = document.createElement("canvas")
  canvas.width = outW
  canvas.height = outH
  const ctx = canvas.getContext("2d")
  if (!ctx) throw new Error("Could not get canvas context")

  ctx.drawImage(
    image,
    pixelCrop.x,
    pixelCrop.y,
    pixelCrop.width,
    pixelCrop.height,
    0,
    0,
    outW,
    outH
  )

  for (const quality of [0.92, 0.8, 0.65, 0.5, 0.35]) {
    const blob = await canvasToBlob(canvas, "image/jpeg", quality)
    if (blob.size <= MAX_COMPRESSED_BYTES) return blob
  }

  // Last resort: scale the canvas down by half and retry
  const small = document.createElement("canvas")
  small.width = Math.round(outW / 2)
  small.height = Math.round(outH / 2)
  const sCtx = small.getContext("2d")
  if (!sCtx) throw new Error("Could not get canvas context")
  sCtx.drawImage(canvas, 0, 0, small.width, small.height)
  return canvasToBlob(small, "image/jpeg", 0.7)
}

export function AvatarUpload({ name, currentPhotoUrl }: AvatarUploadProps) {
  const inputRef = React.useRef<HTMLInputElement>(null)
  const [preview, setPreview] = React.useState<string | null>(null)
  const [isDraggingOver, setIsDraggingOver] = React.useState(false)

  // Crop dialog state
  const [cropSrc, setCropSrc] = React.useState<string | null>(null)
  const [crop, setCrop] = React.useState({ x: 0, y: 0 })
  const [zoom, setZoom] = React.useState(1)
  const [croppedAreaPixels, setCroppedAreaPixels] = React.useState<Area | null>(
    null
  )
  const [isCropping, setIsCropping] = React.useState(false)

  const uploadPhoto = useUploadProfilePhoto()
  const deletePhoto = useDeleteProfilePhoto()
  const { refreshSession } = useAuth()

  const isPending = uploadPhoto.isPending || deletePhoto.isPending || isCropping
  const displaySrc = preview ?? currentPhotoUrl ?? undefined

  React.useEffect(() => {
    return () => {
      if (preview) URL.revokeObjectURL(preview)
      if (cropSrc) URL.revokeObjectURL(cropSrc)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function processFile(file: File) {
    if (!ACCEPTED_MIME_TYPES.has(file.type)) {
      toast.error("Only JPEG, PNG, and WebP images are accepted.")
      return
    }
    const objectUrl = URL.createObjectURL(file)
    setCrop({ x: 0, y: 0 })
    setZoom(1)
    setCroppedAreaPixels(null)
    setCropSrc(objectUrl)
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    e.target.value = ""
    processFile(file)
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault()
    setIsDraggingOver(false)
    if (isPending) return
    const file = e.dataTransfer.files?.[0]
    if (file) processFile(file)
  }

  function handleDragEnter(e: React.DragEvent) {
    e.preventDefault()
    if (!isPending) setIsDraggingOver(true)
  }

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault()
  }

  function handleDragLeave(e: React.DragEvent) {
    if (e.relatedTarget && e.currentTarget.contains(e.relatedTarget as Node))
      return
    setIsDraggingOver(false)
  }

  async function handleApplyCrop() {
    if (!cropSrc || !croppedAreaPixels) return
    setIsCropping(true)
    try {
      const blob = await getCroppedBlob(cropSrc, croppedAreaPixels)
      const file = new File([blob], "photo.jpg", { type: "image/jpeg" })

      URL.revokeObjectURL(cropSrc)
      setCropSrc(null)

      const newPreviewUrl = URL.createObjectURL(blob)
      if (preview) URL.revokeObjectURL(preview)
      setPreview(newPreviewUrl)

      uploadPhoto.mutate(file, {
        onSuccess: () => {
          void refreshSession()
        },
        onError: (err) => {
          URL.revokeObjectURL(newPreviewUrl)
          setPreview(null)
          toast.error(
            err instanceof Error ? err.message : "Failed to upload photo."
          )
        },
      })
    } catch {
      toast.error("Failed to process the image. Please try another file.")
    } finally {
      setIsCropping(false)
    }
  }

  function handleCancelCrop() {
    if (cropSrc) URL.revokeObjectURL(cropSrc)
    setCropSrc(null)
  }

  function handleDelete() {
    deletePhoto.mutate(undefined, {
      onSuccess: () => {
        if (preview) URL.revokeObjectURL(preview)
        setPreview(null)
        void refreshSession()
      },
      onError: (err) => {
        toast.error(
          err instanceof Error ? err.message : "Failed to remove photo."
        )
      },
    })
  }

  const hasPhoto = Boolean(displaySrc)

  return (
    <>
      <div
        className={cn(
          "flex items-center gap-3 rounded-lg transition-colors",
          isDraggingOver && "bg-muted/60"
        )}
        onDrop={handleDrop}
        onDragEnter={handleDragEnter}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
      >
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
              <HugeiconsIcon icon={Edit01Icon} className="size-5 text-white" />
            </button>
          )}
        </div>

        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2 text-sm">
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              disabled={isPending}
              className="font-medium underline-offset-4 hover:underline disabled:opacity-50"
            >
              {hasPhoto ? "Change photo" : "Upload photo"}
            </button>

            {hasPhoto && (
              <>
                <span aria-hidden="true" className="text-muted-foreground">
                  ·
                </span>
                <button
                  type="button"
                  onClick={handleDelete}
                  disabled={isPending}
                  className="flex items-center gap-1 text-muted-foreground underline-offset-4 hover:text-destructive hover:underline disabled:opacity-50"
                >
                  <HugeiconsIcon icon={Cancel01Icon} className="size-3.5" />
                  Remove
                </button>
              </>
            )}
          </div>

          <p className="text-xs text-muted-foreground">
            JPEG, PNG or WebP · compressed to ≤ 500 KB
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

      <Dialog
        open={Boolean(cropSrc)}
        onOpenChange={(open) => {
          if (!open) handleCancelCrop()
        }}
      >
        <DialogContent className="max-w-sm gap-0 overflow-hidden p-0">
          <DialogHeader className="px-6 pt-6 pb-4">
            <DialogTitle>Crop photo</DialogTitle>
          </DialogHeader>

          <div className="relative h-72 bg-black">
            {cropSrc && (
              <Cropper
                image={cropSrc}
                crop={crop}
                zoom={zoom}
                aspect={1}
                cropShape="round"
                showGrid={false}
                onCropChange={setCrop}
                onZoomChange={setZoom}
                onCropComplete={(_, pixels) => setCroppedAreaPixels(pixels)}
              />
            )}
          </div>

          <div className="px-6 py-4">
            <Slider
              min={1}
              max={3}
              step={0.01}
              value={[zoom]}
              onValueChange={([v]) => setZoom(v)}
              aria-label="Zoom"
            />
          </div>

          <DialogFooter className="px-6 pb-6">
            <Button variant="outline" onClick={handleCancelCrop}>
              Cancel
            </Button>
            <Button onClick={handleApplyCrop} disabled={isCropping}>
              {isCropping ? "Processing…" : "Apply"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
