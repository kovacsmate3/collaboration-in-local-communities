"use client"

import * as React from "react"
import { notFound } from "next/navigation"

import { Card } from "@/components/ui/card"
import { ChatList } from "@/components/messages/chat-list"
import { ChatWindow } from "@/components/messages/chat-window"
import { useConversations, useConversationMessages } from "@/lib/api/conversations"

interface ChatPageProps {
  params: Promise<{ chatId: string }>
}

export default function ChatPage({ params }: ChatPageProps) {
  const { chatId } = React.use(params)
  const { data: conversations = [], isLoading: listLoading } = useConversations()
  const { data: messages = [], isLoading: messagesLoading } =
    useConversationMessages(chatId)

  const conversation = conversations.find((c) => c.id === chatId)

  if (!listLoading && !conversation) {
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
            <p className="text-sm text-muted-foreground">Loading…</p>
          </div>
        ) : conversation ? (
          <ChatWindow conversation={conversation} messages={messages} />
        ) : null}
      </Card>
    </div>
  )
}
