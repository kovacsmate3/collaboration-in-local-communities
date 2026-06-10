"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import {
  Add01Icon,
  Cancel01Icon,
  Clock01Icon,
} from "@hugeicons/core-free-icons"
import { useRouter } from "next/navigation"
import type { ComponentProps, SubmitEvent } from "react"
import { toast } from "sonner"
import { useTranslations } from "next-intl"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { AvatarUpload } from "@/components/profile/avatar-upload"
import { LocationInput } from "@/components/shared/location-input"
import { useAuth } from "@/lib/auth-context"
import { toOptionalString } from "@/lib/auth/functions"
import {
  type OwnProfileResponse,
  type SkillResponse,
  useCreateSkill,
  useSkills,
  useUpdateOwnProfile,
} from "@/lib/api/profile"
import type { LocationValue } from "@/lib/location"
import { cn } from "@/lib/utils"

interface ProfileEditFormProps {
  profile: OwnProfileResponse
  selectedSkills: SkillResponse[]
  returnHref?: string
}

interface ProfileFormState {
  displayName: string
  position: string
  workplace: string
  location: LocationValue
  availability: string
  bio: string
  skillIds: string[]
}

type ProfileTextField = Exclude<keyof ProfileFormState, "location" | "skillIds">

export function ProfileEditForm({
  profile,
  selectedSkills,
  returnHref = "/profile",
}: ProfileEditFormProps) {
  const t = useTranslations("profile.editForm")
  const router = useRouter()
  const { refreshSession } = useAuth()
  const updateProfile = useUpdateOwnProfile()
  const [localSkills, setLocalSkills] = React.useState<SkillResponse[]>([])
  const [form, setForm] = React.useState<ProfileFormState>(() => ({
    displayName: profile.displayName,
    position: profile.position ?? "",
    workplace: profile.workplace ?? "",
    location: {
      locationText: profile.locationText ?? "",
      latitude: profile.latitude ?? undefined,
      longitude: profile.longitude ?? undefined,
    },
    availability: profile.availability ?? "",
    bio: profile.bio ?? "",
    skillIds: profile.skillIds,
  }))

  const skillById = React.useMemo(() => {
    const map = new Map(selectedSkills.map((s) => [s.id, s]))
    for (const s of localSkills) map.set(s.id, s)
    return map
  }, [selectedSkills, localSkills])

  function updateField(field: ProfileTextField, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function updateLocation(location: LocationValue) {
    setForm((current) => ({ ...current, location }))
  }

  function addSkill(skill: SkillResponse) {
    setLocalSkills((prev) =>
      prev.some((s) => s.id === skill.id) ? prev : [...prev, skill]
    )
    setForm((current) =>
      current.skillIds.includes(skill.id)
        ? current
        : { ...current, skillIds: [...current.skillIds, skill.id] }
    )
  }

  function removeSkill(id: string) {
    setForm((current) => ({
      ...current,
      skillIds: current.skillIds.filter((skillId) => skillId !== id),
    }))
  }

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()

    const displayName = form.displayName.trim()
    if (!displayName) {
      toast.error(t("fullNameRequired"))
      return
    }

    const { latitude, longitude } = form.location
    if ((latitude === undefined) !== (longitude === undefined)) {
      toast.error(t("latLngTogether"))
      return
    }

    try {
      await updateProfile.mutateAsync({
        displayName,
        position: toOptionalString(form.position),
        workplace: toOptionalString(form.workplace),
        locationText: toOptionalString(form.location.locationText),
        latitude,
        longitude,
        availability: toOptionalString(form.availability),
        bio: toOptionalString(form.bio),
        skillIds: form.skillIds,
      })
      await refreshSession()
      toast.success(t("savedToast"))
      router.push(returnHref)
      router.refresh()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : t("saveError"))
    }
  }

  const hasPendingSkills = form.skillIds.some(
    (id) => skillById.get(id)?.status === "Pending"
  )

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <AvatarUpload
        name={form.displayName || profile.displayName}
        currentPhotoUrl={profile.photoUrl}
      />

      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          id="displayName"
          label={t("fullName")}
          value={form.displayName}
          onChange={(e) => updateField("displayName", e.target.value)}
          required
        />
        <Field
          id="position"
          label={t("role")}
          value={form.position}
          onChange={(e) => updateField("position", e.target.value)}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          id="workplace"
          label={t("workplace")}
          value={form.workplace}
          onChange={(e) => updateField("workplace", e.target.value)}
        />
        <LocationInput
          id="location"
          label={t("location")}
          value={form.location}
          onChange={updateLocation}
        />
      </div>

      <Field
        id="availability"
        label={t("availability")}
        value={form.availability}
        onChange={(e) => updateField("availability", e.target.value)}
      />

      <div className="flex flex-col gap-2">
        <Label htmlFor="bio">{t("bio")}</Label>
        <Textarea
          id="bio"
          rows={4}
          value={form.bio}
          onChange={(e) => updateField("bio", e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-3">
        <Label>{t("skills")}</Label>
        <SkillCombobox selectedIds={new Set(form.skillIds)} onAdd={addSkill} />
        {form.skillIds.length > 0 && (
          <ul className="flex flex-wrap gap-1.5">
            {form.skillIds.map((id) => {
              const skill = skillById.get(id)
              const isPending = skill?.status === "Pending"
              return (
                <li key={id}>
                  <Badge
                    variant="muted"
                    className={cn("h-7 gap-1 pr-1", isPending && "opacity-70")}
                  >
                    {isPending && (
                      <HugeiconsIcon
                        icon={Clock01Icon}
                        className="size-3"
                        strokeWidth={2}
                      />
                    )}
                    {skill?.name ?? id}
                    <button
                      type="button"
                      onClick={() => removeSkill(id)}
                      className="grid size-5 place-items-center rounded-full hover:bg-background"
                      aria-label={
                        skill?.name
                          ? t("removeSkill", { name: skill.name })
                          : t("removeSkillFallback")
                      }
                    >
                      <HugeiconsIcon icon={Cancel01Icon} className="size-3" />
                    </button>
                  </Badge>
                </li>
              )
            })}
          </ul>
        )}
        {hasPendingSkills && (
          <p className="text-xs text-muted-foreground">
            {t("pendingSkillsHint")}
          </p>
        )}
      </div>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={() => router.push(returnHref)}
          disabled={updateProfile.isPending}
        >
          {t("cancel")}
        </Button>
        <Button type="submit" disabled={updateProfile.isPending}>
          {updateProfile.isPending ? t("saving") : t("save")}
        </Button>
      </div>
    </form>
  )
}

// ── SkillCombobox ─────────────────────────────────────────────────────────────

interface SkillComboboxProps {
  selectedIds: Set<string>
  onAdd: (skill: SkillResponse) => void
}

function SkillCombobox({ selectedIds, onAdd }: SkillComboboxProps) {
  const t = useTranslations("profile.editForm")
  const [query, setQuery] = React.useState("")
  const [open, setOpen] = React.useState(false)
  const [activeIndex, setActiveIndex] = React.useState(-1)
  const inputRef = React.useRef<HTMLInputElement>(null)
  const createSkill = useCreateSkill()

  const trimmedQuery = query.trim()
  const skillsQuery = useSkills(trimmedQuery)

  const available = React.useMemo(
    () => (skillsQuery.data ?? []).filter((s) => !selectedIds.has(s.id)),
    [skillsQuery.data, selectedIds]
  )

  const exactMatch = (skillsQuery.data ?? []).some(
    (s) => s.name.toLowerCase() === trimmedQuery.toLowerCase()
  )
  const canCreate = trimmedQuery.length >= 2 && !exactMatch
  const showDropdown = open && trimmedQuery.length > 0
  const totalItems = available.length + (canCreate ? 1 : 0)

  function select(skill: SkillResponse) {
    onAdd(skill)
    setQuery("")
    setOpen(false)
    setActiveIndex(-1)
    inputRef.current?.focus()
  }

  async function create() {
    if (!canCreate || createSkill.isPending) return
    try {
      const skill = await createSkill.mutateAsync({ name: trimmedQuery })
      onAdd(skill)
      setQuery("")
      setOpen(false)
      setActiveIndex(-1)
      inputRef.current?.focus()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t("skillAddError"))
    }
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (!showDropdown) {
      if (e.key === "ArrowDown" && trimmedQuery.length > 0) {
        setOpen(true)
      }
      return
    }

    if (e.key === "ArrowDown") {
      e.preventDefault()
      setActiveIndex((i) => (i < totalItems - 1 ? i + 1 : i))
    } else if (e.key === "ArrowUp") {
      e.preventDefault()
      setActiveIndex((i) => (i > 0 ? i - 1 : 0))
    } else if (e.key === "Enter") {
      e.preventDefault()
      if (activeIndex >= 0 && activeIndex < available.length) {
        select(available[activeIndex])
      } else if (canCreate && activeIndex === available.length) {
        void create()
      } else if (available.length === 1 && activeIndex < 0) {
        select(available[0])
      } else if (available.length === 0 && canCreate) {
        void create()
      }
    } else if (e.key === "Escape") {
      setOpen(false)
      setActiveIndex(-1)
    }
  }

  return (
    <div className="relative">
      <Input
        ref={inputRef}
        value={query}
        placeholder={t("skillSearchPlaceholder")}
        autoComplete="off"
        onChange={(e) => {
          setQuery(e.target.value)
          setOpen(true)
          setActiveIndex(-1)
        }}
        onFocus={() => {
          if (trimmedQuery.length > 0) setOpen(true)
        }}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        onKeyDown={handleKeyDown}
      />

      {showDropdown && (
        <div
          role="listbox"
          aria-label={t("skillSuggestionsAria")}
          className="absolute z-50 mt-1 w-full overflow-hidden rounded-md border bg-popover text-popover-foreground shadow-md"
        >
          {skillsQuery.isFetching && (
            <p className="px-3 py-2.5 text-sm text-muted-foreground">
              {t("skillSearching")}
            </p>
          )}

          {!skillsQuery.isFetching && available.length === 0 && !canCreate && (
            <p className="px-3 py-2.5 text-sm text-muted-foreground">
              {t("skillsNoResults")}
            </p>
          )}

          {!skillsQuery.isFetching &&
            available.map((skill, i) => (
              <button
                key={skill.id}
                type="button"
                role="option"
                aria-selected={activeIndex === i}
                onMouseDown={(e) => {
                  e.preventDefault()
                  select(skill)
                }}
                onMouseEnter={() => setActiveIndex(i)}
                className={cn(
                  "flex w-full items-center px-3 py-2.5 text-left text-sm transition-colors",
                  activeIndex === i
                    ? "bg-accent text-accent-foreground"
                    : "hover:bg-accent hover:text-accent-foreground"
                )}
              >
                {skill.name}
              </button>
            ))}

          {canCreate && (
            <button
              type="button"
              role="option"
              aria-selected={activeIndex === available.length}
              disabled={createSkill.isPending}
              onMouseDown={(e) => {
                e.preventDefault()
                void create()
              }}
              onMouseEnter={() => setActiveIndex(available.length)}
              className={cn(
                "flex w-full items-center gap-2 px-3 py-2.5 text-left text-sm font-medium transition-colors disabled:opacity-50",
                available.length > 0 && "border-t",
                activeIndex === available.length
                  ? "bg-accent text-accent-foreground"
                  : "hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <HugeiconsIcon
                icon={Add01Icon}
                className="size-4 shrink-0"
                strokeWidth={2}
              />
              {createSkill.isPending
                ? t("skillAdding")
                : t("skillAddOption", { name: trimmedQuery })}
            </button>
          )}
        </div>
      )}
    </div>
  )
}

// ── Field ─────────────────────────────────────────────────────────────────────

interface FieldProps extends ComponentProps<typeof Input> {
  id: string
  label: string
}

function Field({ id, label, className, ...rest }: FieldProps) {
  return (
    <div className="flex flex-1 flex-col gap-2">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} className={className} {...rest} />
    </div>
  )
}
