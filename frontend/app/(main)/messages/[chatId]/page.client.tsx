"use client"

import * as React from "react"
import { notFound } from "next/navigation"
import { useTranslations } from "next-intl"

import { Card } from "@/components/ui/card"
import { ChatList } from "@/components/messages/chat-list"
import { ChatWindow } from "@/components/messages/chat-window"
import {
  useConversations,
  useConversationMessages,
  useMarkConversationRead,
} from "@/lib/api/conversations"

interface ChatPageClientProps {
  chatId: string
}

export function ChatPageClient({ chatId }: ChatPageClientProps) {
  const t = useTranslations("messages.page")
  const {
    data: conversations = [],
    isLoading: listLoading,
    isFetching: listFetching,
  } = useConversations()
  const {
    data: messagesData,
    isLoading: messagesLoading,
    hasPreviousPage,
    fetchPreviousPage,
    isFetchingPreviousPage,
  } = useConversationMessages(chatId)

  const messages = messagesData?.pages.flatMap((p) => p.messages) ?? []
  const lastMessageId = messages[messages.length - 1]?.id
  const { mutate: markRead } = useMarkConversationRead()

  React.useEffect(() => {
    if (chatId) markRead(chatId)
  }, [chatId, lastMessageId, markRead])

  const conversation = conversations.find((c) => c.id === chatId)

  if (!listLoading && !listFetching && !conversation) {
    notFound()
  }

  return (
    <div className="grid h-[calc(100svh-9rem)] grid-cols-1 gap-4 md:grid-cols-[18rem_1fr]">
      <Card className="hidden overflow-hidden p-0 md:flex md:flex-col">
        <ChatList conversations={conversations} activeId={chatId} />
      </Card>

      <Card className="overflow-hidden p-0">
        {listLoading || messagesLoading ? (
          <div className="flex h-full items-center justify-center">
            <p className="text-sm text-muted-foreground">{t("loading")}</p>
          </div>
        ) : conversation ? (
          <ChatWindow
            conversation={conversation}
            messages={messages}
            hasPreviousPage={hasPreviousPage}
            fetchPreviousPage={fetchPreviousPage}
            isFetchingPreviousPage={isFetchingPreviousPage}
          />
        ) : null}
      </Card>
    </div>
  )
}
