"use client"

import * as React from "react"
import { useRouter } from "next/navigation"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { User } from "@/lib/types"

interface ProfileEditFormProps {
  user: User
}

/**
 * Editable profile form (US-18 privacy partial: visibility toggles per
 * field will be added in a follow-up - they live alongside each input
 * once we integrate them).
 *
 * Submit handler is a stub: replace with a server action / mutation.
 */
export function ProfileEditForm({ user }: ProfileEditFormProps) {
  const router = useRouter()

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    // TODO: submit profile update
    router.push("/profile")
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="name" label="Full name" defaultValue={user.name} required />
        <Field
          id="position"
          label="Role"
          defaultValue={user.position ?? ""}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field
          id="workplace"
          label="Workplace / school"
          defaultValue={user.workplace ?? ""}
        />
        <Field
          id="location"
          label="Location"
          defaultValue={user.location ?? ""}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="bio">Bio</Label>
        <Textarea id="bio" rows={4} defaultValue={user.bio ?? ""} />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="skills">Skills</Label>
        <Input
          id="skills"
          defaultValue={user.skills.join(", ")}
          placeholder="Comma-separated"
        />
      </div>

      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="ghost"
          onClick={() => router.push("/profile")}
        >
          Cancel
        </Button>
        <Button type="submit">Save changes</Button>
      </div>
    </form>
  )
}

interface FieldProps extends React.ComponentProps<typeof Input> {
  id: string
  label: string
}

function Field({ id, label, ...rest }: FieldProps) {
  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor={id}>{label}</Label>
      <Input id={id} {...rest} />
    </div>
  )
}
