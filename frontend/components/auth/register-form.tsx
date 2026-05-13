"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { useState, type SubmitEvent } from "react"
import { useForm, type FieldErrors } from "react-hook-form"
import { toast } from "sonner"

import { RegisterAccountStep } from "@/components/auth/register-account-step"
import { RegisterProfileStep } from "@/components/auth/register-profile-step"
import { RegisterStepIndicator } from "@/components/auth/register-step-indicator"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Form } from "@/components/ui/form"
import { useAuth } from "@/lib/auth-context"
import { APP_AUTH_ROUTES } from "@/lib/auth/constants"
import {
  getAuthErrorMessage,
  getRegisterSubmitLabel,
  type RegistrationStep,
} from "@/lib/auth/functions"
import { toRegisterInput } from "@/lib/auth/mappers"
import {
  REGISTER_FORM_DEFAULT_VALUES,
  registerSchema,
  REGISTER_ACCOUNT_FIELDS,
  type RegisterFormValues,
} from "@/lib/auth/schemas"

export function RegisterForm() {
  const { register } = useAuth()
  const router = useRouter()
  const [step, setStep] = useState<RegistrationStep>("account")

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: REGISTER_FORM_DEFAULT_VALUES,
    mode: "onTouched",
  })

  const isSubmitting = form.formState.isSubmitting
  const serverError = form.formState.errors.root?.message

  async function handleFormSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    form.clearErrors("root")

    if (step === "account") {
      const accountFieldsAreValid = await form.trigger(
        REGISTER_ACCOUNT_FIELDS,
        { shouldFocus: true }
      )

      if (accountFieldsAreValid) {
        setStep("profile")
      }

      return
    }

    await form.handleSubmit(onSubmit, onInvalidSubmit)(event)
  }

  async function onSubmit(values: RegisterFormValues) {
    form.clearErrors("root")

    try {
      await register(toRegisterInput(values))
      toast.success("Account created successfully")
      router.replace(APP_AUTH_ROUTES.login)
      router.refresh()
    } catch (error) {
      const message = getAuthErrorMessage(error, "Unable to create account.")
      form.setError("root", { message })
      toast.error(message)
    }
  }

  function onInvalidSubmit(errors: FieldErrors<RegisterFormValues>) {
    setStep(hasAccountErrors(errors) ? "account" : "profile")
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
          <Form {...form}>
            <form noValidate onSubmit={handleFormSubmit} className="grid gap-6">
              <RegisterStepIndicator step={step} />

              {step === "account" ? (
                <RegisterAccountStep />
              ) : (
                <RegisterProfileStep />
              )}

              {serverError ? (
                <p
                  role="alert"
                  className="text-sm font-medium text-destructive"
                >
                  {serverError}
                </p>
              ) : null}

              <div className="grid gap-2">
                <Button
                  type="submit"
                  disabled={isSubmitting}
                  className="w-full"
                >
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
          </Form>
        </CardContent>
      </Card>
    </div>
  )
}

function hasAccountErrors(errors: FieldErrors<RegisterFormValues>): boolean {
  return REGISTER_ACCOUNT_FIELDS.some((field) => Boolean(errors[field]))
}
