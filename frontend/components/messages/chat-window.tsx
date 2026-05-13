"use client"

import * as React from "react"
import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { Sent02Icon } from "@hugeicons/core-free-icons"
import type { SubmitEvent } from "react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import { MessageBubble } from "@/components/messages/message-bubble"
import { UserAvatar } from "@/components/shared/user-avatar"
import { useAuth } from "@/lib/auth-context"
import { useSendMessage } from "@/lib/api/conversations"
import type { ApiConversationPreview, ApiMessage } from "@/lib/api/conversations"
import { useConversationHub } from "@/lib/chat-hub"

interface ChatWindowProps {
  conversation: ApiConversationPreview
  messages: ApiMessage[]
}

export function ChatWindow({ conversation, messages }: ChatWindowProps) {
  const { user } = useAuth()
  const [draft, setDraft] = React.useState("")
  const bottomRef = React.useRef<HTMLDivElement>(null)
  const { mutate: sendMessage, isPending } = useSendMessage(conversation.id)

  useConversationHub(conversation.id, user?.profileId)

  React.useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" })
  }, [messages])

  function handleSend(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    const content = draft.trim()
    if (!content) return
    setDraft("")
    sendMessage(content)
  }

  const { otherParticipant, taskId, taskTitle } = conversation

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-center gap-3 border-b border-border px-4 py-3">
        <UserAvatar
          name={otherParticipant.displayName}
          src={otherParticipant.photoUrl ?? undefined}
        />
        <div className="flex min-w-0 flex-1 flex-col">
          <span className="truncate text-sm font-medium">
            {otherParticipant.displayName}
          </span>
          <Link
            href={`/tasks/${taskId}`}
            className="truncate text-xs text-muted-foreground hover:text-foreground hover:underline"
          >
            {taskTitle}
          </Link>
        </div>
      </header>

      <ScrollArea className="flex-1 px-4">
        <div className="flex flex-col gap-2 py-4">
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
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Message…"
          aria-label="Message"
          disabled={isPending}
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
