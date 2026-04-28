import type { Metadata } from "next"

import { PageHeader } from "@/components/shared/page-header"
import { ChatList } from "@/components/messages/chat-list"
import { Card } from "@/components/ui/card"
import { mockChats } from "@/lib/mock-data"

export const metadata: Metadata = {
  title: "Messages",
}

/**
 * Messages overview (Module 6).
 *
 * Shows the list of conversations. On desktop you could promote this to
 * a master/detail layout, but for the skeleton we keep list-only and
 * navigate to `/messages/[id]` for the conversation view.
 */
export default function MessagesPage() {
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Messages"
        description="Direct chats opened after a task is accepted. History is preserved for dispute reference."
      />
      <Card className="overflow-hidden p-0">
        <ChatList chats={mockChats} />
      </Card>
    </div>
  )
}
