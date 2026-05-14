"use client"

import { PageHeader } from "@/components/shared/page-header"
import { ChatList } from "@/components/messages/chat-list"
import { Card } from "@/components/ui/card"
import { useConversations } from "@/lib/api/conversations"

export function MessagesPageClient() {
  const { data: conversations = [], isLoading, isError } = useConversations()

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Messages"
        description="Your conversations about tasks."
      />
      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">
          Could not load conversations. Please try again.
        </p>
      ) : (
        <Card className="overflow-hidden p-0">
          <ChatList conversations={conversations} />
        </Card>
      )}
    </div>
  )
}
