"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import { ICON_REGISTRY } from "@/lib/icon-registry"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"

interface IconPickerProps {
  value: string
  onChange: (iconName: string) => void
  disabled?: boolean
}

export function IconPicker({ value, onChange, disabled }: IconPickerProps) {
  const [open, setOpen] = React.useState(false)
  const [search, setSearch] = React.useState("")

  const allIcons = Object.entries(ICON_REGISTRY)
  const filtered = allIcons.filter(([name]) =>
    name.toLowerCase().includes(search.toLowerCase())
  )

  const selectedIcon = value
    ? ICON_REGISTRY[value as keyof typeof ICON_REGISTRY]
    : undefined

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          className="w-full justify-start gap-2 font-normal"
        >
          {selectedIcon ? (
            <>
              <HugeiconsIcon
                icon={selectedIcon}
                className="size-4 shrink-0"
                strokeWidth={1.5}
              />
              <span className="truncate text-sm">{value}</span>
            </>
          ) : (
            <span className="text-muted-foreground">Select an icon…</span>
          )}
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Choose an icon</DialogTitle>
          <DialogDescription className="sr-only">
            Select an icon to represent this category.
          </DialogDescription>
        </DialogHeader>
        <Input
          placeholder="Search icons…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          autoFocus
        />
        <div className="grid max-h-64 grid-cols-4 gap-2 overflow-y-auto pr-1">
          {filtered.map(([name, icon]) => (
            <button
              key={name}
              type="button"
              onClick={() => {
                onChange(name)
                setOpen(false)
              }}
              className={cn(
                "flex flex-col items-center gap-1.5 rounded-md border p-2.5 text-center transition-colors hover:bg-muted",
                value === name && "border-primary bg-primary/10 text-primary"
              )}
            >
              <HugeiconsIcon icon={icon} className="size-5" strokeWidth={1.5} />
              <span className="w-full truncate text-[9px] leading-tight text-muted-foreground">
                {name.replace("Icon", "")}
              </span>
            </button>
          ))}
          {filtered.length === 0 && (
            <p className="col-span-4 py-6 text-center text-sm text-muted-foreground">
              No icons match &ldquo;{search}&rdquo;
            </p>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
