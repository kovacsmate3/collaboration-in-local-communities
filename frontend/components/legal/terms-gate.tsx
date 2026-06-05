"use client"

import Link from "next/link"
import { useRouter } from "next/navigation"
import { toast } from "sonner"
import { useTranslations } from "next-intl"

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
  const t = useTranslations("legal")
  const tGate = useTranslations("legal.gate")
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
      toast.error(t("termsNotAvailableToast"))
      return
    }

    try {
      await acceptTerms.mutateAsync(termsVersionId)
      await refreshSession()
      toast.success(t("termsAcceptedToast"))
      router.refresh()
    } catch {
      toast.error(t("recordFailedToast"))
    }
  }

  return (
    <AlertDialog open>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{tGate("title")}</AlertDialogTitle>
          <AlertDialogDescription>
            {tGate("description")}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <div className="rounded-md border bg-muted/40 px-3 py-2 text-sm">
          {currentUser.terms.activeTitle ?? t("termsTitleFallback")}
          {currentUser.terms.activeVersion ? (
            <span className="text-muted-foreground">
              {" "}
              {t("versionLabel", { version: currentUser.terms.activeVersion })}
            </span>
          ) : null}
        </div>
        <AlertDialogFooter>
          <Button asChild variant="outline">
            <Link href={APP_LEGAL_ROUTES.terms}>{tGate("reviewTerms")}</Link>
          </Button>
          <Button
            type="button"
            disabled={acceptTerms.isPending}
            onClick={() => void handleAcceptTerms()}
          >
            {acceptTerms.isPending ? tGate("accepting") : tGate("accept")}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
