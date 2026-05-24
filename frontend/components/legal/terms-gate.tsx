"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { useAcceptTerms } from "@/lib/api/terms"
import { useAuth } from "@/lib/auth-context"
import { APP_LEGAL_ROUTES } from "@/lib/auth/constants"

export function TermsGate() {
  const { user, refreshSession } = useAuth()
  const acceptTerms = useAcceptTerms()
  const router = useRouter()

  if (!user || user.terms.hasAccepted) {
    return null
  }

  const currentUser = user

  async function handleAcceptTerms() {
    const termsVersionId = currentUser.terms.activeVersionId
    if (!termsVersionId) {
      toast.error("Terms are not available right now. Please try again later.")
      return
    }

    try {
      await acceptTerms.mutateAsync(termsVersionId)
      await refreshSession()
      toast.success("Terms accepted")
      router.refresh()
    } catch {
      toast.error("Unable to record your acceptance. Please try again.")
    }
  }

  return (
    <AlertDialog open>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Accept Terms & Conditions</AlertDialogTitle>
          <AlertDialogDescription>
            Please accept the current terms before continuing to use the app.
            You can review them first if you need a closer look.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="rounded-md border bg-muted/40 px-3 py-2 text-sm">
          {currentUser.terms.activeTitle ?? "Current terms"}
          {currentUser.terms.activeVersion ? (
            <span className="text-muted-foreground">
              {" "}
              version {currentUser.terms.activeVersion}
            </span>
          ) : null}
        </div>
        <AlertDialogFooter>
          <Button asChild variant="outline">
            <Link href={APP_LEGAL_ROUTES.terms}>Review terms</Link>
          </Button>
          <Button
            type="button"
            disabled={acceptTerms.isPending}
            onClick={() => void handleAcceptTerms()}
          >
            {acceptTerms.isPending ? "Accepting..." : "Accept"}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
