import Link from "next/link"
import { BubbleChatIcon } from "@hugeicons/core-free-icons"

import { UserAvatar } from "@/components/shared/user-avatar"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import { cn } from "@/lib/utils"
import type { ChatPreview } from "@/lib/types"

interface ChatListProps {
  chats: ChatPreview[]
  /** When set, the matching chat is visually highlighted (used in split view). */
  activeId?: string
}

/**
 * Vertical list of chat previews. The active row is highlighted to
 * support a master/detail layout on desktop.
 */
export function ChatList({ chats, activeId }: ChatListProps) {
  if (chats.length === 0) {
    return (
      <EmptyState
        icon={BubbleChatIcon}
        title="No conversations yet"
        description="When you accept a task, a chat will open here."
      />
    )
  }

  return (
    <ul className="flex flex-col">
      {chats.map((chat) => {
        const isActive = chat.id === activeId
        return (
          <li key={chat.id}>
            <Link
              href={`/messages/${chat.id}`}
              className={cn(
                "flex items-start gap-3 border-b border-border px-4 py-3 transition-colors last:border-b-0",
                isActive ? "bg-muted" : "hover:bg-muted/60"
              )}
            >
              <UserAvatar
                name={chat.participant.name}
                src={chat.participant.avatarUrl}
              />
              <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                <div className="flex items-baseline justify-between gap-2">
                  <span className="truncate text-sm font-medium">
                    {chat.participant.name}
                  </span>
                  <span className="shrink-0 text-xs text-muted-foreground">
                    {formatRelativeTime(chat.lastMessageAt)}
                  </span>
                </div>
                <p className="truncate text-xs text-muted-foreground">
                  {chat.taskTitle}
                </p>
                <p className="truncate text-sm text-muted-foreground">
                  {chat.lastMessage}
                </p>
              </div>
              {chat.unreadCount > 0 ? (
                <span
                  className="flex size-5 shrink-0 items-center justify-center rounded-full bg-primary text-[0.65rem] font-medium text-primary-foreground"
                  aria-label={`${chat.unreadCount} unread`}
                >
                  {chat.unreadCount}
                </span>
              ) : null}
            </Link>
          </li>
        )
      })}
    </ul>
  )
}
