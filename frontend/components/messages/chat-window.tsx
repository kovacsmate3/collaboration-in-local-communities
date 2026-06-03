"use client"

import * as React from "react"
import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { Sent02Icon } from "@hugeicons/core-free-icons"
import type { SubmitEvent } from "react"

import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import { MessageBubble } from "@/components/messages/message-bubble"
import { UserAvatar } from "@/components/shared/user-avatar"
import { useAuth } from "@/lib/auth-context"
import { useSendMessage } from "@/lib/api/conversations"
import type {
  ApiConversationPreview,
  ApiMessage,
} from "@/lib/api/conversations"
import { usePatchTaskApplication } from "@/lib/api/tasks"
import { useConversationHub } from "@/lib/chat-hub"

interface ChatWindowProps {
  conversation: ApiConversationPreview
  messages: ApiMessage[]
  hasPreviousPage: boolean
  fetchPreviousPage: () => void
  isFetchingPreviousPage: boolean
}

export function ChatWindow({
  conversation,
  messages,
  hasPreviousPage,
  fetchPreviousPage,
  isFetchingPreviousPage,
}: ChatWindowProps) {
  const { user } = useAuth()
  const [draft, setDraft] = React.useState("")
  const bottomRef = React.useRef<HTMLDivElement>(null)
  const inputRef = React.useRef<HTMLInputElement>(null)
  const { mutate: sendMessage, isPending } = useSendMessage(conversation.id)
  const { mutate: patchApplication, isPending: isPatchingApplication } =
    usePatchTaskApplication(conversation.taskId)

  useConversationHub(conversation.id, user?.profileId)

  function handleAccept() {
    if (!conversation.pendingApplicationId) return
    patchApplication(
      { applicationId: conversation.pendingApplicationId, action: "accept" },
      {
        onSuccess: () => toast.success("Application accepted."),
        onError: () => toast.error("Could not accept the application."),
      }
    )
  }

  function handleReject() {
    if (!conversation.pendingApplicationId) return
    patchApplication(
      { applicationId: conversation.pendingApplicationId, action: "reject" },
      {
        onSuccess: () => toast.success("Application rejected."),
        onError: () => toast.error("Could not reject the application."),
      }
    )
  }

  const lastMessageId = messages[messages.length - 1]?.id

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" })
  }, [lastMessageId])

  function handleSend(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    if (isPending) return
    const content = draft.trim()
    if (!content) return
    setDraft("")
    inputRef.current?.focus()
    sendMessage(content)
  }

  const { otherParticipant, taskId, taskTitle } = conversation

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-center gap-3 border-b border-border px-4 py-3">
        <Link
          href={`/profile/${otherParticipant.profileId}`}
          className="flex min-w-0 flex-1 items-center gap-3"
        >
          <UserAvatar
            name={otherParticipant.displayName}
            src={otherParticipant.photoUrl ?? undefined}
          />
          <span className="truncate text-sm font-medium hover:underline">
            {otherParticipant.displayName}
          </span>
        </Link>
        <Link
          href={`/tasks/${taskId}`}
          className="min-w-0 truncate text-xs text-muted-foreground hover:text-foreground hover:underline"
        >
          {taskTitle}
        </Link>
      </header>

      {conversation.pendingApplicationId ? (
        <div className="flex items-center gap-3 border-b border-border bg-muted/40 px-4 py-2.5">
          <p className="flex-1 text-sm text-muted-foreground">
            {conversation.otherParticipant.displayName} applied to help with
            this task.
          </p>
          <Button
            size="sm"
            disabled={isPatchingApplication}
            onClick={handleAccept}
          >
            Accept
          </Button>
          <Button
            size="sm"
            variant="ghost"
            disabled={isPatchingApplication}
            onClick={handleReject}
          >
            Decline
          </Button>
        </div>
      ) : null}

      <ScrollArea className="flex-1 px-4">
        <div className="flex flex-col gap-2 py-4">
          {hasPreviousPage && (
            <div className="flex justify-center py-1">
              <Button
                variant="ghost"
                size="sm"
                onClick={fetchPreviousPage}
                disabled={isFetchingPreviousPage}
              >
                {isFetchingPreviousPage ? "Loading…" : "Load older messages"}
              </Button>
            </div>
          )}
          {messages.length === 0 ? (
            <p className="text-center text-xs text-muted-foreground">
              No messages yet. Say hello!
            </p>
          ) : null}
          {messages.map((m) => (
            <MessageBubble key={m.id} message={m} />
          ))}
          <div ref={bottomRef} />
        </div>
      </ScrollArea>

      <form
        onSubmit={handleSend}
        className="flex items-center gap-2 border-t border-border px-4 py-3"
      >
        <Input
          ref={inputRef}
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Message…"
          aria-label="Message"
        />
        <Button
          type="submit"
          size="icon"
          disabled={isPending || !draft.trim()}
          aria-label="Send"
        >
          <HugeiconsIcon icon={Sent02Icon} />
        </Button>
      </form>
    </div>
  )
}
