"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { useSearchParams } from "next/navigation"
import { useState, type SubmitEvent } from "react"
import { useForm } from "react-hook-form"
import { toast } from "sonner"

import { PasswordField } from "@/components/forms/password-field"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Form } from "@/components/ui/form"
import { APP_AUTH_ROUTES, AUTH_API_PATHS } from "@/lib/auth/constants"
import {
  RESET_PASSWORD_FORM_DEFAULT_VALUES,
  resetPasswordSchema,
  type ResetPasswordFormValues,
} from "@/lib/auth/schemas"

export function ResetPasswordForm() {
  const searchParams = useSearchParams()
  const userId = searchParams.get("userId")
  const token = searchParams.get("token")
  const [succeeded, setSucceeded] = useState(false)

  const form = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: RESET_PASSWORD_FORM_DEFAULT_VALUES,
    mode: "onTouched",
  })

  const isSubmitting = form.formState.isSubmitting
  const serverError = form.formState.errors.root?.message

  if (!userId || !token) {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Invalid reset link</CardTitle>
          <CardDescription>
            This password reset link is incomplete or has expired.
          </CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3">
          <Button asChild variant="outline" className="w-full">
            <Link href={APP_AUTH_ROUTES.forgotPassword}>
              Request a new link
            </Link>
          </Button>
          <Button asChild variant="ghost" className="w-full">
            <Link href={APP_AUTH_ROUTES.login}>Back to sign in</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  if (succeeded) {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Password updated</CardTitle>
          <CardDescription>
            Your password has been reset. You can now sign in with your new
            password.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button asChild className="w-full">
            <Link href={APP_AUTH_ROUTES.login}>Sign in</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  async function onSubmit(values: ResetPasswordFormValues) {
    form.clearErrors("root")
    const response = await fetch(AUTH_API_PATHS.resetPassword, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId,
        token,
        newPassword: values.newPassword,
      }),
      cache: "no-store",
    })

    if (!response.ok) {
      form.setError("root", {
        message:
          "This link is invalid or has expired. Please request a new one.",
      })
      return
    }

    setSucceeded(true)
    toast.success("Password updated successfully.")
  }

  function handleFormSubmit(event: SubmitEvent<HTMLFormElement>) {
    void form.handleSubmit(onSubmit)(event)
  }

  return (
    <Form {...form}>
      <form noValidate onSubmit={handleFormSubmit} className="grid gap-6">
        <PasswordField<ResetPasswordFormValues>
          name="newPassword"
          label="New password"
          autoComplete="new-password"
        />

        <PasswordField<ResetPasswordFormValues>
          name="confirmPassword"
          label="Confirm new password"
          autoComplete="new-password"
        />

        {serverError ? (
          <div role="alert" className="grid gap-2">
            <p className="text-sm font-medium text-destructive">
              {serverError}
            </p>
            <Button asChild variant="outline" size="sm">
              <Link href={APP_AUTH_ROUTES.forgotPassword}>
                Request a new link
              </Link>
            </Button>
          </div>
        ) : null}

        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? "Updating..." : "Update password"}
        </Button>

        <p className="text-center text-sm text-muted-foreground">
          <Link
            href={APP_AUTH_ROUTES.login}
            className="font-medium text-foreground hover:underline"
          >
            Back to sign in
          </Link>
        </p>
      </form>
    </Form>
  )
}
