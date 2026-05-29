"use client"

import * as React from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import {
  type ApiTaskApplication,
  useApplyToTask,
  useWithdrawTaskApplication,
} from "@/lib/api/tasks"

interface ApplicationControlsProps {
  taskId: string
  application?: ApiTaskApplication
  isLoadingApplication?: boolean
}

/**
 * Helper-side controls for a task: apply, view pending application, or
 * withdraw. Renders different affordances based on the helper's current
 * application status.
 */
export function ApplicationControls({
  taskId,
  application,
  isLoadingApplication = false,
}: ApplicationControlsProps) {
  const router = useRouter()
  const [open, setOpen] = React.useState(false)
  const [message, setMessage] = React.useState("")
  const { mutate: applyToTask, isPending: isApplying } = useApplyToTask(taskId)
  const { mutate: withdraw, isPending: isWithdrawing } =
    useWithdrawTaskApplication(taskId)

  function handleApply(event: React.SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()

    applyToTask(
      { message: message.trim() || undefined },
      {
        onSuccess: (app) => {
          setMessage("")
          setOpen(false)
          if (app.conversationId) {
            router.push(`/messages/${app.conversationId}`)
          } else {
            toast.success("Application sent.")
          }
        },
        onError: () => toast.error("Could not apply to this task."),
      }
    )
  }

  function handleWithdraw() {
    if (!application) return

    withdraw(application.id, {
      onSuccess: () => toast.success("Application withdrawn."),
      onError: () => toast.error("Could not withdraw the application."),
    })
  }

  if (!application && isLoadingApplication) {
    return <Button disabled>Apply to help</Button>
  }

  if (application?.status === "Pending") {
    return (
      <>
        <Button variant="outline" disabled>
          Application pending
        </Button>
        {application.conversationId ? (
          <Button variant="outline" asChild>
            <Link href={`/messages/${application.conversationId}`}>
              View conversation
            </Link>
          </Button>
        ) : null}
        <Button
          variant="ghost"
          disabled={isWithdrawing}
          onClick={handleWithdraw}
        >
          {isWithdrawing ? "Withdrawing…" : "Withdraw"}
        </Button>
      </>
    )
  }

  if (application) {
    return (
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="outline" className="h-9 rounded-md px-3">
          Application {application.status.toLowerCase()}
        </Badge>
        {application.conversationId ? (
          <Button variant="outline" size="sm" asChild>
            <Link href={`/messages/${application.conversationId}`}>
              View conversation
            </Link>
          </Button>
        ) : null}
      </div>
    )
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>Apply to help</Button>
      </DialogTrigger>
      <DialogContent className="max-w-md">
        <form onSubmit={handleApply}>
          <DialogHeader>
            <DialogTitle>Apply to help</DialogTitle>
            <DialogDescription>
              Send a short note to the seeker with your availability or relevant
              experience.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Textarea
              value={message}
              onChange={(event) => setMessage(event.target.value)}
              maxLength={1000}
              placeholder="I can help this afternoon and have a small hand cart."
              rows={5}
            />
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isApplying}>
              {isApplying ? "Sending…" : "Send application"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
