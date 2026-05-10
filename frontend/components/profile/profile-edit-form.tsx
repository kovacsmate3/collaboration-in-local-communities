"use client"

import * as React from "react"
import { HugeiconsIcon } from "@hugeicons/react"
import { Add01Icon, Cancel01Icon } from "@hugeicons/core-free-icons"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { LocationInput } from "@/components/shared/location-input"
import { UserAvatar } from "@/components/shared/user-avatar"
import { useAuth } from "@/lib/auth-context"
import { toOptionalString } from "@/lib/auth/functions"
import {
  type OwnProfileResponse,
  type SkillResponse,
  useSkills,
  useUpdateOwnProfile,
} from "@/lib/api/profile"
import type { LocationValue } from "@/lib/location"

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
  photoUrl: string
  bio: string
  skillIds: string[]
}

type ProfileTextField = Exclude<keyof ProfileFormState, "location" | "skillIds">

export function ProfileEditForm({
  profile,
  selectedSkills,
  returnHref = "/profile",
}: ProfileEditFormProps) {
  const router = useRouter()
  const { refreshSession } = useAuth()
  const updateProfile = useUpdateOwnProfile()
  const [skillPrefix, setSkillPrefix] = React.useState("")
  const skillsQuery = useSkills(skillPrefix)
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
    photoUrl: profile.photoUrl ?? "",
    bio: profile.bio ?? "",
    skillIds: profile.skillIds,
  }))

  const skillById = React.useMemo(() => {
    const next = new Map(selectedSkills.map((skill) => [skill.id, skill]))
    for (const skill of skillsQuery.data ?? []) {
      next.set(skill.id, skill)
    }
    return next
  }, [selectedSkills, skillsQuery.data])

  function updateField(field: ProfileTextField, value: string) {
    setForm((current) => ({ ...current, [field]: value }))
  }

  function updateLocation(location: LocationValue) {
    setForm((current) => ({ ...current, location }))
  }

  function addSkill(id: string) {
    setForm((current) =>
      current.skillIds.includes(id)
        ? current
        : { ...current, skillIds: [...current.skillIds, id] }
    )
  }

  function removeSkill(id: string) {
    setForm((current) => ({
      ...current,
      skillIds: current.skillIds.filter((skillId) => skillId !== id),
    }))
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()

    const displayName = form.displayName.trim()
    if (!displayName) {
      toast.error("Full name is required.")
      return
    }

    const { latitude, longitude } = form.location
    if ((latitude === undefined) !== (longitude === undefined)) {
      toast.error("Latitude and longitude must be provided together.")
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
        photoUrl: toOptionalString(form.photoUrl),
        bio: toOptionalString(form.bio),
        skillIds: form.skillIds,
      })
      await refreshSession()
      toast.success("Profile saved")
      router.push(returnHref)
      router.refresh()
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Unable to save profile."
      )
    }
  }

  const selectedSkillSet = new Set(form.skillIds)
  const availableSkills =
    skillsQuery.data?.filter((skill) => !selectedSkillSet.has(skill.id)) ?? []

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <div className="flex items-center gap-4">
        <UserAvatar
          name={form.displayName || profile.displayName}
          src={toOptionalString(form.photoUrl)}
          size="lg"
        />
        <Field
          id="photoUrl"
          label="Photo URL"
          inputMode="url"
          value={form.photoUrl}
          onChange={(e) => updateField("photoUrl", e.target.value)}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          id="displayName"
          label="Full name"
          value={form.displayName}
          onChange={(e) => updateField("displayName", e.target.value)}
          required
        />
        <Field
          id="position"
          label="Role"
          value={form.position}
          onChange={(e) => updateField("position", e.target.value)}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          id="workplace"
          label="Workplace / school"
          value={form.workplace}
          onChange={(e) => updateField("workplace", e.target.value)}
        />
        <LocationInput
          id="location"
          label="Location"
          value={form.location}
          onChange={updateLocation}
        />
      </div>

      <Field
        id="availability"
        label="Availability"
        value={form.availability}
        onChange={(e) => updateField("availability", e.target.value)}
      />

      <div className="flex flex-col gap-2">
        <Label htmlFor="bio">Bio</Label>
        <Textarea
          id="bio"
          rows={4}
          value={form.bio}
          onChange={(e) => updateField("bio", e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-3">
        <Label htmlFor="skill-search">Skills</Label>
        <div className="flex flex-col gap-2 sm:flex-row">
          <Input
            id="skill-search"
            value={skillPrefix}
            onChange={(e) => setSkillPrefix(e.target.value)}
          />
          <Select onValueChange={addSkill}>
            <SelectTrigger className="w-full sm:w-56">
              <SelectValue placeholder="Add skill" />
            </SelectTrigger>
            <SelectContent>
              {availableSkills.length > 0 ? (
                availableSkills.map((skill) => (
                  <SelectItem key={skill.id} value={skill.id}>
                    {skill.name}
                  </SelectItem>
                ))
              ) : (
                <SelectItem value="empty" disabled>
                  No matches
                </SelectItem>
              )}
            </SelectContent>
          </Select>
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              const first = availableSkills[0]
              if (first) {
                addSkill(first.id)
              }
            }}
            disabled={availableSkills.length === 0}
            aria-label="Add first matching skill"
          >
            <HugeiconsIcon icon={Add01Icon} className="size-4" />
          </Button>
        </div>
        {form.skillIds.length > 0 ? (
          <ul className="flex flex-wrap gap-1.5">
            {form.skillIds.map((id) => {
              const skill = skillById.get(id)
              return (
                <li key={id}>
                  <Badge variant="muted" className="h-7 gap-1 pr-1">
                    {skill?.name ?? id}
                    <button
                      type="button"
                      onClick={() => removeSkill(id)}
                      className="grid size-5 place-items-center rounded-full hover:bg-background"
                      aria-label={`Remove ${skill?.name ?? "skill"}`}
                    >
                      <HugeiconsIcon icon={Cancel01Icon} className="size-3" />
                    </button>
                  </Badge>
                </li>
              )
            })}
          </ul>
        ) : null}
      </div>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={() => router.push(returnHref)}
          disabled={updateProfile.isPending}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={updateProfile.isPending}>
          {updateProfile.isPending ? "Saving..." : "Save changes"}
        </Button>
      </div>
    </form>
  )
}

interface FieldProps extends React.ComponentProps<typeof Input> {
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
