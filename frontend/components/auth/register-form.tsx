"use client"

import * as React from "react"
import Link from "next/link"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"

/**
 * Two-step registration covered as a single page for the skeleton.
 *
 * Real product should split this into:
 *   1. Account creation (email, password, T&C acceptance)
 *   2. Profile completion (name, photo, bio, skills, location)
 *
 * The user becomes both a Seeker and a Helper by default (US-06).
 */
export function RegisterForm() {
  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    // TODO: integrate auth provider + uploader
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="name">Full name</Label>
          <Input id="name" required autoComplete="name" />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" required autoComplete="email" />
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="password">Password</Label>
        <Input
          id="password"
          type="password"
          required
          autoComplete="new-password"
          minLength={8}
        />
        <p className="text-xs text-muted-foreground">
          At least 8 characters. Mix letters and numbers.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="workplace">Workplace / school</Label>
          <Input id="workplace" placeholder="ELTE, Acme Co., …" />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="position">Role</Label>
          <Input id="position" placeholder="Student, Designer, …" />
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="location">Location</Label>
        <Input id="location" placeholder="City, neighbourhood" />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="bio">Short bio</Label>
        <Textarea
          id="bio"
          rows={3}
          placeholder="Tell others a little about yourself."
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="skills">Skills</Label>
        <Input
          id="skills"
          placeholder="e.g. Math tutoring, Furniture assembly, Pet sitting"
        />
        <p className="text-xs text-muted-foreground">
          Comma-separated. Skills help match you to relevant tasks.
        </p>
      </div>

      <label className="flex items-start gap-2 text-sm text-muted-foreground">
        <input
          type="checkbox"
          required
          className="mt-1 size-4 rounded border-input"
        />
        <span>
          I agree to the{" "}
          <Link
            href="/terms"
            className="font-medium text-foreground hover:underline"
          >
            Terms &amp; Conditions
          </Link>{" "}
          and the Community Guidelines.
        </span>
      </label>

      <Button type="submit">Create account</Button>

      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <Link
          href="/login"
          className="font-medium text-foreground hover:underline"
        >
          Sign in
        </Link>
      </p>
    </form>
  )
}
