"use client"

import * as React from "react"
import Link from "next/link"
import { useSearchParams } from "next/navigation"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { APP_AUTH_ROUTES, AUTH_API_PATHS } from "@/lib/auth/constants"

type Status = "loading" | "success" | "error"

export function VerifyEmailForm() {
  const searchParams = useSearchParams()
  const userId = searchParams.get("userId")
  const token = searchParams.get("token")
  const hasParams = Boolean(userId && token)
  const [status, setStatus] = React.useState<Status>(
    hasParams ? "loading" : "error"
  )
  const [errorMessage, setErrorMessage] = React.useState(
    hasParams
      ? "The link has expired or is invalid."
      : "The verification link is incomplete."
  )

  React.useEffect(() => {
    if (!userId || !token) return

    const controller = new AbortController()

    void fetch(AUTH_API_PATHS.confirmEmail, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ userId, token }),
      cache: "no-store",
      signal: controller.signal,
    })
      .then(async (response) => {
        if (response.ok) {
          setStatus("success")
          return
        }

        try {
          const body: unknown = await response.json()
          if (typeof body === "string") setErrorMessage(body)
        } catch {
          // keep default message
        }
        setStatus("error")
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        console.error("Email confirmation failed", error)
        setErrorMessage("Something went wrong. Please try again.")
        setStatus("error")
      })

    return () => {
      controller.abort()
    }
  }, [searchParams, userId, token])

  if (status === "loading") {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Verifying your email…</CardTitle>
          <CardDescription>Please wait a moment.</CardDescription>
        </CardHeader>
      </Card>
    )
  }

  if (status === "success") {
    return (
      <Card>
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Email verified</CardTitle>
          <CardDescription>
            Your account is active. You can now sign in.
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

  return (
    <Card>
      <CardHeader className="text-center">
        <CardTitle className="text-xl">Verification failed</CardTitle>
        <CardDescription>{errorMessage}</CardDescription>
      </CardHeader>
      <CardContent className="grid gap-3">
        <Button asChild variant="outline" className="w-full">
          <Link href={APP_AUTH_ROUTES.register}>Create a new account</Link>
        </Button>
        <Button asChild variant="ghost" className="w-full">
          <Link href={APP_AUTH_ROUTES.login}>Back to sign in</Link>
        </Button>
      </CardContent>
    </Card>
  )
}
