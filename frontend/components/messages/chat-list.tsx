"use client"

import Link from "next/link"
import { BubbleChatIcon } from "@hugeicons/core-free-icons"
import { useLocale, useTranslations } from "next-intl"

import { UserAvatar } from "@/components/shared/user-avatar"
import { EmptyState } from "@/components/shared/empty-state"
import { formatRelativeTime } from "@/lib/format"
import { cn } from "@/lib/utils"
import type { ApiConversationPreview } from "@/lib/api/conversations"

interface ChatListProps {
  conversations: ApiConversationPreview[]
  activeId?: string
}

export function ChatList({ conversations, activeId }: ChatListProps) {
  const t = useTranslations("messages.list")
  const locale = useLocale()

  if (conversations.length === 0) {
    return (
      <EmptyState
        icon={BubbleChatIcon}
        title={t("emptyTitle")}
        description={t("emptyBody")}
      />
    )
  }

  return (
    <ul className="flex flex-col">
      {conversations.map((c) => {
        const isActive = c.id === activeId
        return (
          <li key={c.id}>
            <Link
              href={`/messages/${c.id}`}
              className={cn(
                "flex items-start gap-3 border-b border-border px-4 py-3 transition-colors last:border-b-0",
                isActive ? "bg-muted" : "hover:bg-muted/60"
              )}
            >
              <UserAvatar
                name={c.otherParticipant.displayName}
                src={c.otherParticipant.photoUrl ?? undefined}
              />
              <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                <div className="flex items-baseline justify-between gap-2">
                  <div className="flex min-w-0 items-center gap-1.5">
                    <span className="truncate text-sm font-medium">
                      {c.otherParticipant.displayName}
                    </span>
                    {c.hasUnread && (
                      <>
                        <span
                          className="h-2 w-2 shrink-0 rounded-full bg-primary"
                          aria-hidden="true"
                        />
                        <span className="sr-only">{t("unread")}</span>
                      </>
                    )}
                  </div>
                  {c.lastMessageAt ? (
                    <span className="shrink-0 text-xs text-muted-foreground">
                      {formatRelativeTime(c.lastMessageAt, locale)}
                    </span>
                  ) : null}
                </div>
                <p className="truncate text-xs text-muted-foreground">
                  {c.taskTitle}
                </p>
                {c.lastMessageContent ? (
                  <p className="truncate text-sm text-muted-foreground">
                    {c.lastMessageContent}
                  </p>
                ) : null}
              </div>
            </Link>
          </li>
        )
      })}
    </ul>
  )
}
