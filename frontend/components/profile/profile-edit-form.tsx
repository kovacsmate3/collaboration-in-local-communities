"use client"

import * as React from "react"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Separator } from "@/components/ui/separator"
import {
  useUpdateProfile,
  useUpdatePrivacySettings,
  type ApiOwnProfile,
  type ApiPrivacySettings,
} from "@/lib/api/profiles"

interface ProfileEditFormProps {
  profile: ApiOwnProfile
}

export function ProfileEditForm({ profile }: ProfileEditFormProps) {
  const router = useRouter()
  const { mutateAsync: updateProfile } = useUpdateProfile()
  const { mutateAsync: updatePrivacy } = useUpdatePrivacySettings()
  const [isPending, setIsPending] = React.useState(false)

  const [form, setForm] = React.useState({
    displayName: profile.displayName,
    bio: profile.bio ?? "",
    workplace: profile.workplace ?? "",
    position: profile.position ?? "",
    availability: profile.availability ?? "",
    locationText: profile.locationText ?? "",
  })

  const [privacy, setPrivacy] = React.useState<ApiPrivacySettings>(
    profile.privacySettings
  )

  function updateField(key: keyof typeof form, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  function togglePrivacy(key: keyof ApiPrivacySettings) {
    setPrivacy((prev) => ({ ...prev, [key]: !prev[key] }))
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setIsPending(true)
    try {
      await Promise.all([
        updateProfile({
          displayName: form.displayName,
          bio: form.bio || null,
          workplace: form.workplace || null,
          position: form.position || null,
          availability: form.availability || null,
          locationText: form.locationText || null,
        }),
        updatePrivacy(privacy),
      ])
      toast.success("Profile updated.")
      router.push("/profile")
    } catch {
      toast.error("Could not save changes. Please try again.")
    } finally {
      setIsPending(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="displayName">Full name</Label>
          <Input
            id="displayName"
            required
            value={form.displayName}
            onChange={(e) => updateField("displayName", e.target.value)}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="position">Role</Label>
          <Input
            id="position"
            value={form.position}
            onChange={(e) => updateField("position", e.target.value)}
          />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="workplace">Workplace / school</Label>
          <Input
            id="workplace"
            value={form.workplace}
            onChange={(e) => updateField("workplace", e.target.value)}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="locationText">Location</Label>
          <Input
            id="locationText"
            placeholder="City, neighbourhood"
            value={form.locationText}
            onChange={(e) => updateField("locationText", e.target.value)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="availability">Availability</Label>
        <Input
          id="availability"
          placeholder="e.g. Weekends, evenings after 6 PM"
          value={form.availability}
          onChange={(e) => updateField("availability", e.target.value)}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="bio">Bio</Label>
        <Textarea
          id="bio"
          rows={4}
          value={form.bio}
          onChange={(e) => updateField("bio", e.target.value)}
        />
      </div>

      <Separator />

      <div className="flex flex-col gap-3">
        <p className="text-sm font-medium">Privacy</p>
        <p className="text-xs text-muted-foreground">
          Control which fields are visible on your public profile.
        </p>
        <div className="grid gap-3 sm:grid-cols-2">
          <PrivacyToggle
            id="showWorkplace"
            label="Show workplace"
            checked={privacy.showWorkplace}
            onChange={() => togglePrivacy("showWorkplace")}
          />
          <PrivacyToggle
            id="showPosition"
            label="Show role"
            checked={privacy.showPosition}
            onChange={() => togglePrivacy("showPosition")}
          />
          <PrivacyToggle
            id="showLocation"
            label="Show location"
            checked={privacy.showLocation}
            onChange={() => togglePrivacy("showLocation")}
          />
          <PrivacyToggle
            id="showAvailability"
            label="Show availability"
            checked={privacy.showAvailability}
            onChange={() => togglePrivacy("showAvailability")}
          />
        </div>
      </div>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          disabled={isPending}
          onClick={() => router.push("/profile")}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={isPending}>
          {isPending ? "Saving…" : "Save changes"}
        </Button>
      </div>
    </form>
  )
}

function PrivacyToggle({
  id,
  label,
  checked,
  onChange,
}: {
  id: string
  label: string
  checked: boolean
  onChange: () => void
}) {
  return (
    <label
      htmlFor={id}
      className="flex cursor-pointer items-center justify-between rounded-md border border-border px-3 py-2 text-sm hover:bg-muted"
    >
      <span>{label}</span>
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className="size-4 rounded border-input accent-primary"
      />
    </label>
  )
}
