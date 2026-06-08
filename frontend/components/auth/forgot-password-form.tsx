"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { type SubmitEvent } from "react"
import { useForm } from "react-hook-form"
import { useTranslations } from "next-intl"

import { TextField } from "@/components/forms/text-field"
import { Button } from "@/components/ui/button"
import { Form } from "@/components/ui/form"
import { APP_AUTH_ROUTES, AUTH_API_PATHS } from "@/lib/auth/constants"
import {
  FORGOT_PASSWORD_FORM_DEFAULT_VALUES,
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from "@/lib/auth/schemas"

export function ForgotPasswordForm() {
  const t = useTranslations("auth.forgot")
  const form = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: FORGOT_PASSWORD_FORM_DEFAULT_VALUES,
    mode: "onTouched",
  })

  const { isSubmitting, isSubmitSuccessful, errors } = form.formState

  async function onSubmit(values: ForgotPasswordFormValues) {
    try {
      await fetch(AUTH_API_PATHS.forgotPassword, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: values.email }),
        cache: "no-store",
      })
      // Always treat HTTP responses as success to prevent email enumeration.
    } catch {
      form.setError("root", {
        message: t("networkError"),
      })
    }
  }

  function handleFormSubmit(event: SubmitEvent<HTMLFormElement>) {
    void form.handleSubmit(onSubmit)(event)
  }

  if (isSubmitSuccessful) {
    return (
      <div className="flex flex-col gap-4">
        <p className="text-sm text-muted-foreground">{t("sentNotice")}</p>
        <p className="text-center text-sm text-muted-foreground">
          <Link
            href={APP_AUTH_ROUTES.login}
            className="font-medium text-foreground hover:underline"
          >
            {t("backToSignIn")}
          </Link>
        </p>
      </div>
    )
  }

  return (
    <Form {...form}>
      <form
        noValidate
        onSubmit={handleFormSubmit}
        className="flex flex-col gap-4"
      >
        <TextField<ForgotPasswordFormValues>
          name="email"
          label={t("emailLabel")}
          type="email"
          autoComplete="email"
          placeholder={t("emailPlaceholder")}
          description={t("emailDescription")}
        />

        {errors.root ? (
          <p role="alert" className="text-sm font-medium text-destructive">
            {errors.root.message}
          </p>
        ) : null}

        <Button type="submit" disabled={isSubmitting} className="mt-2">
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
