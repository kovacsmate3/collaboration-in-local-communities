"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { useSearchParams } from "next/navigation"
import { useState, type SubmitEvent } from "react"
import { useForm } from "react-hook-form"
import { useTranslations } from "next-intl"
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
  const t = useTranslations("auth.reset")
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
          <CardTitle className="text-xl">{t("invalidLinkTitle")}</CardTitle>
          <CardDescription>{t("invalidLinkSubtitle")}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-3">
          <Button asChild variant="outline" className="w-full">
            <Link href={APP_AUTH_ROUTES.forgotPassword}>
              {t("requestNewLink")}
            </Link>
          </Button>
          <Button asChild variant="ghost" className="w-full">
            <Link href={APP_AUTH_ROUTES.login}>{t("backToSignIn")}</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  if (succeeded) {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">{t("successTitle")}</CardTitle>
          <CardDescription>{t("successSubtitle")}</CardDescription>
        </CardHeader>
        <CardContent>
          <Button asChild className="w-full">
            <Link href={APP_AUTH_ROUTES.login}>{t("signIn")}</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  async function onSubmit(values: ResetPasswordFormValues) {
    form.clearErrors()
    let response: Response
    try {
      response = await fetch(AUTH_API_PATHS.resetPassword, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          userId,
          token,
          newPassword: values.newPassword,
        }),
        cache: "no-store",
      })
    } catch {
      form.setError("root", {
        message: t("networkError"),
      })
      return
    }

    if (response.ok) {
      setSucceeded(true)
      toast.success(t("successToast"))
      return
    }

    if (response.status === 400 || response.status === 422) {
      let body: unknown
      try {
        body = await response.json()
      } catch {
        body = undefined
      }
      const errors =
        typeof body === "object" && body !== null
          ? (body as { errors?: Record<string, string[]> }).errors
          : undefined
      if (errors) {
        const firstError = Object.values(errors).find(
          (msgs) => msgs.length > 0
        )?.[0]
        form.setError("newPassword", {
          message: firstError ?? t("invalidPasswordError"),
        })
      } else {
        form.setError("root", {
          message: t("linkExpiredError"),
        })
      }
      return
    }

    form.setError("root", {
      message: t("linkExpiredError"),
    })
  }

  function handleFormSubmit(event: SubmitEvent<HTMLFormElement>) {
    void form.handleSubmit(onSubmit)(event)
  }

  return (
    <Form {...form}>
      <form noValidate onSubmit={handleFormSubmit} className="grid gap-6">
        <PasswordField<ResetPasswordFormValues>
          name="newPassword"
          label={t("newPasswordLabel")}
          autoComplete="new-password"
        />

        <PasswordField<ResetPasswordFormValues>
          name="confirmPassword"
          label={t("confirmPasswordLabel")}
          autoComplete="new-password"
        />

        {serverError ? (
          <div role="alert" className="grid gap-2">
            <p className="text-sm font-medium text-destructive">
              {serverError}
            </p>
            <Button asChild variant="outline" size="sm">
              <Link href={APP_AUTH_ROUTES.forgotPassword}>
                {t("requestNewLink")}
              </Link>
            </Button>
          </div>
        ) : null}

        <Button type="submit" disabled={isSubmitting} className="w-full">
          {isSubmitting ? t("submitting") : t("submit")}
        </Button>

        <p className="text-center text-sm text-muted-foreground">
          <Link
            href={APP_AUTH_ROUTES.login}
            className="font-medium text-foreground hover:underline"
          >
            {t("backToSignIn")}
          </Link>
        </p>
      </form>
    </Form>
  )
}
