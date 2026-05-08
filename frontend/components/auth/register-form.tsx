"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES, APP_LEGAL_ROUTES } from "@/lib/auth/constants"
import {
  getAuthErrorMessage,
  getHomePathForRole,
  getRegisterSubmitLabel,
  toOptionalString,
  type RegistrationStep,
} from "@/lib/auth/functions"

interface RegisterFormState {
  email: string
  password: string
  acceptTerms: boolean
  displayName: string
  workplace: string
  position: string
  locationText: string
  bio: string
}

const INITIAL_FORM: RegisterFormState = {
  email: "",
  password: "",
  acceptTerms: false,
  displayName: "",
  workplace: "",
  position: "",
  locationText: "",
  bio: "",
}

export function RegisterForm() {
  const { register } = useAuth()
  const router = useRouter()
  const [step, setStep] = React.useState<RegistrationStep>("account")
  const [form, setForm] = React.useState<RegisterFormState>(INITIAL_FORM)
  const [isSubmitting, setIsSubmitting] = React.useState(false)

  function update<K extends keyof RegisterFormState>(
    key: K,
    value: RegisterFormState[K]
  ) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (step === "account") {
      setStep("profile")
      return
    }

    setIsSubmitting(true)

    try {
      const user = await register({
        email: form.email,
        password: form.password,
        acceptTerms: form.acceptTerms,
        displayName: form.displayName,
        workplace: toOptionalString(form.workplace),
        position: toOptionalString(form.position),
        locationText: toOptionalString(form.locationText),
        bio: toOptionalString(form.bio),
      })

      toast.success("Account created successfully")
      router.replace(getHomePathForRole(user.role))
      router.refresh()
    } catch (error) {
      toast.error(getAuthErrorMessage(error, "Unable to create account."))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Create your account</CardTitle>
          <CardDescription>
            {step === "account"
              ? "Start with your sign-in details"
              : "Complete your public profile"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="grid gap-6">
            <StepIndicator step={step} />

            {step === "account" ? (
              <AccountStep form={form} update={update} />
            ) : (
              <ProfileStep form={form} update={update} />
            )}

            <div className="grid gap-2">
              <Button type="submit" disabled={isSubmitting} className="w-full">
                {getRegisterSubmitLabel(step, isSubmitting)}
              </Button>
              {step === "profile" ? (
                <Button
                  type="button"
                  variant="ghost"
                  disabled={isSubmitting}
                  onClick={() => setStep("account")}
                >
                  Back
                </Button>
              ) : null}
            </div>

            <p className="text-center text-sm text-muted-foreground">
              Already have an account?{" "}
              <Link
                href={APP_AUTH_ROUTES.login}
                className="font-medium text-foreground underline-offset-4 hover:underline"
              >
                Sign in
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}

function AccountStep({
  form,
  update,
}: {
  form: RegisterFormState
  update: <K extends keyof RegisterFormState>(
    key: K,
    value: RegisterFormState[K]
  ) => void
}) {
  return (
    <div className="grid gap-4">
      <div className="grid gap-3">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          name="email"
          type="email"
          required
          autoComplete="email"
          placeholder="you@example.com"
          value={form.email}
          onChange={(event) => update("email", event.target.value)}
        />
      </div>

      <div className="grid gap-3">
        <Label htmlFor="password">Password</Label>
        <Input
          id="password"
          name="password"
          type="password"
          required
          autoComplete="new-password"
          minLength={8}
          value={form.password}
          onChange={(event) => update("password", event.target.value)}
        />
        <p className="text-xs text-muted-foreground">At least 8 characters.</p>
      </div>

      <label className="flex items-start gap-2 text-sm text-muted-foreground">
        <input
          type="checkbox"
          required
          checked={form.acceptTerms}
          onChange={(event) => update("acceptTerms", event.target.checked)}
          className="mt-1 size-4 rounded border-input"
        />
        <span>
          I agree to the{" "}
          <Link
            href={APP_LEGAL_ROUTES.terms}
            className="font-medium text-foreground underline-offset-4 hover:underline"
          >
            Terms
          </Link>
          .
        </span>
      </label>
    </div>
  )
}

function ProfileStep({
  form,
  update,
}: {
  form: RegisterFormState
  update: <K extends keyof RegisterFormState>(
    key: K,
    value: RegisterFormState[K]
  ) => void
}) {
  return (
    <div className="grid gap-4">
      <div className="grid gap-3">
        <Label htmlFor="displayName">Full name</Label>
        <Input
          id="displayName"
          name="displayName"
          required
          autoComplete="name"
          value={form.displayName}
          onChange={(event) => update("displayName", event.target.value)}
        />
      </div>

      <div className="grid gap-3">
        <Label htmlFor="workplace">Workplace / school</Label>
        <Input
          id="workplace"
          name="workplace"
          value={form.workplace}
          onChange={(event) => update("workplace", event.target.value)}
        />
      </div>

      <div className="grid gap-3">
        <Label htmlFor="position">Role</Label>
        <Input
          id="position"
          name="position"
          value={form.position}
          onChange={(event) => update("position", event.target.value)}
        />
      </div>

      <div className="grid gap-3">
        <Label htmlFor="locationText">Location</Label>
        <Input
          id="locationText"
          name="locationText"
          placeholder="City, neighbourhood"
          value={form.locationText}
          onChange={(event) => update("locationText", event.target.value)}
        />
      </div>

      <div className="grid gap-3">
        <Label htmlFor="bio">Short bio</Label>
        <Textarea
          id="bio"
          name="bio"
          rows={3}
          value={form.bio}
          onChange={(event) => update("bio", event.target.value)}
        />
      </div>
    </div>
  )
}

function StepIndicator({ step }: { step: RegistrationStep }) {
  return (
    <div className="grid grid-cols-2 gap-2 text-center text-xs font-medium">
      <span
        className={
          step === "account"
            ? "rounded-md bg-primary px-3 py-2 text-primary-foreground"
            : "rounded-md bg-muted px-3 py-2 text-muted-foreground"
        }
      >
        Account
      </span>
      <span
        className={
          step === "profile"
            ? "rounded-md bg-primary px-3 py-2 text-primary-foreground"
            : "rounded-md bg-muted px-3 py-2 text-muted-foreground"
        }
      >
        Profile
      </span>
    </div>
  )
}
