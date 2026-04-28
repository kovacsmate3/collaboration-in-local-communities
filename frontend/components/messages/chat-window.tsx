"use client"

import * as React from "react"
import Link from "next/link"
import { HugeiconsIcon } from "@hugeicons/react"
import { Sent02Icon } from "@hugeicons/core-free-icons"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import { MessageBubble } from "@/components/messages/message-bubble"
import { UserAvatar } from "@/components/shared/user-avatar"
import type { ChatMessage, ChatPreview } from "@/lib/types"

interface ChatWindowProps {
  chat: ChatPreview
  messages: ChatMessage[]
}

/**
 * Three-row chat surface: header (participant + linked task),
 * scrollable history, composer.
 *
 * The composer is local-state only - swap in a real socket/mutation
 * when the backend is ready.
 */
export function ChatWindow({ chat, messages }: ChatWindowProps) {
  const [draft, setDraft] = React.useState("")

  const handleSend = (e: React.FormEvent) => {
    e.preventDefault()
    if (!draft.trim()) return
    // TODO: send message via API/socket
    setDraft("")
  }

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-center gap-3 border-b border-border px-4 py-3">
        <UserAvatar
          name={chat.participant.name}
          src={chat.participant.avatarUrl}
        />
        <div className="flex min-w-0 flex-1 flex-col">
          <span className="truncate text-sm font-medium">
            {chat.participant.name}
          </span>
          <Link
            href={`/tasks/${chat.taskId}`}
            className="truncate text-xs text-muted-foreground hover:text-foreground hover:underline"
          >
            {chat.taskTitle}
          </Link>
        </div>
      </header>

      <ScrollArea className="flex-1 px-4">
        <div className="flex flex-col gap-2 py-4">
          {messages.map((m) => (
            <MessageBubble key={m.id} message={m} />
          ))}
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
        />
        <Button type="submit" size="icon" aria-label="Send">
          <HugeiconsIcon icon={Sent02Icon} />
        </Button>
      </form>
    </div>
  )
}
