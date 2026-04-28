import { notFound } from "next/navigation"

import { Card } from "@/components/ui/card"
import { ChatList } from "@/components/messages/chat-list"
import { ChatWindow } from "@/components/messages/chat-window"
import { getChatById, mockChats, mockMessages } from "@/lib/mock-data"

interface ChatPageProps {
  params: Promise<{ chatId: string }>
}

/**
 * Conversation view with master/detail layout.
 *
 * - Mobile: only the chat window is shown (back button takes you to the list)
 * - md+: the list is visible to the left for fast switching
 */
export default async function ChatPage({ params }: ChatPageProps) {
  const { chatId } = await params
  const chat = getChatById(chatId)
  if (!chat) notFound()

  return (
    <div className="grid h-[calc(100svh-9rem)] grid-cols-1 gap-4 md:grid-cols-[18rem_1fr]">
      <Card className="hidden overflow-hidden p-0 md:flex md:flex-col">
        <ChatList chats={mockChats} activeId={chatId} />
      </Card>

      <Card className="overflow-hidden p-0">
        {/* TODO: scope messages to the chat once the API is wired */}
        <ChatWindow chat={chat} messages={mockMessages} />
      </Card>
    </div>
  )
}
