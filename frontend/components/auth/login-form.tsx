"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { useRouter, useSearchParams } from "next/navigation"
import type { SubmitEvent } from "react"
import { useForm } from "react-hook-form"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { PasswordField } from "@/components/forms/password-field"
import { TextField } from "@/components/forms/text-field"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Form } from "@/components/ui/form"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES, APP_LEGAL_ROUTES } from "@/lib/auth/constants"
import {
  getAuthErrorMessage,
  getPostAuthRedirectPath,
} from "@/lib/auth/functions"
import {
  LOGIN_FORM_DEFAULT_VALUES,
  loginSchema,
  type LoginFormValues,
} from "@/lib/auth/schemas"

export function LoginForm() {
  const { login } = useAuth()
  const router = useRouter()
  const searchParams = useSearchParams()

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: LOGIN_FORM_DEFAULT_VALUES,
    mode: "onTouched",
  })

  const isSubmitting = form.formState.isSubmitting
  const serverError = form.formState.errors.root?.message

  async function onSubmit(values: LoginFormValues) {
    form.clearErrors("root")
    try {
      const user = await login(values)
      toast.success("Signed in successfully")
      router.replace(
        getPostAuthRedirectPath(searchParams.get("next"), user.role)
      )
      router.refresh()
    } catch (error) {
      const message = getAuthErrorMessage(error, "Unable to sign in.")
      form.setError("root", { message })
      toast.error(message)
    }
  }

  function handleFormSubmit(event: SubmitEvent<HTMLFormElement>) {
    form.clearErrors("root")
    void form.handleSubmit(onSubmit)(event)
  }

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Welcome back</CardTitle>
          <CardDescription>
            Sign in with your email and password
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form noValidate onSubmit={handleFormSubmit} className="grid gap-6">
              <TextField<LoginFormValues>
                name="email"
                label="Email"
                type="email"
                autoComplete="email"
                placeholder="you@example.com"
              />

              <PasswordField<LoginFormValues>
                name="password"
                label="Password"
                autoComplete="current-password"
                labelAction={
                  <Link
                    href={APP_AUTH_ROUTES.forgotPassword}
                    className="text-xs text-muted-foreground underline-offset-4 hover:text-foreground hover:underline"
                  >
                    Forgot password?
                  </Link>
                }
              />

              {serverError ? (
                <p
                  role="alert"
                  className="text-sm font-medium text-destructive"
                >
                  {serverError}
                </p>
              ) : null}

              <Button type="submit" disabled={isSubmitting} className="w-full">
                {isSubmitting ? "Signing in..." : "Sign in"}
              </Button>

              <p className="text-center text-sm text-muted-foreground">
                New here?{" "}
                <Link
                  href={APP_AUTH_ROUTES.register}
                  className="font-medium text-foreground underline-offset-4 hover:underline"
                >
                  Create an account
                </Link>
              </p>
            </form>
          </Form>
        </CardContent>
      </Card>
      <p className="px-6 text-center text-xs text-balance text-muted-foreground">
        By continuing, you agree to our{" "}
        <Link
          href={APP_LEGAL_ROUTES.terms}
          className="underline underline-offset-4"
        >
          Terms
        </Link>
        .
      </p>
    </div>
  )
}
