"use client"

import * as React from "react"
import { notFound } from "next/navigation"

import { Card } from "@/components/ui/card"
import { ChatList } from "@/components/messages/chat-list"
import { ChatWindow } from "@/components/messages/chat-window"
import { ErrorState } from "@/components/shared/error-state"
import { LoadingState } from "@/components/shared/loading-state"
import {
  useConversations,
  useConversationMessages,
  useMarkConversationRead,
} from "@/lib/api/conversations"

interface ChatPageClientProps {
  chatId: string
}

export function ChatPageClient({ chatId }: ChatPageClientProps) {
  const {
    data: conversations = [],
    isLoading: listLoading,
    isFetching: listFetching,
    isError: listError,
    refetch: refetchList,
  } = useConversations()
  const {
    data: messagesData,
    isLoading: messagesLoading,
    isError: messagesError,
    refetch: refetchMessages,
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

  if (!listLoading && !listFetching && !listError && !conversation) {
    notFound()
  }

  const fetchError = listError || messagesError
  const isFetching = listLoading || messagesLoading

  function handleRetry() {
    if (listError) void refetchList()
    if (messagesError) void refetchMessages()
  }

  return (
    <div className="grid h-[calc(100svh-9rem)] grid-cols-1 gap-4 md:grid-cols-[18rem_1fr]">
      <Card className="hidden overflow-hidden p-0 md:flex md:flex-col">
        <ChatList conversations={conversations} activeId={chatId} />
      </Card>

      <Card className="overflow-hidden p-0">
        {isFetching ? (
          <div className="p-4">
            <LoadingState rows={4} />
          </div>
        ) : fetchError ? (
          <div className="p-4">
            <ErrorState onRetry={handleRetry} />
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
