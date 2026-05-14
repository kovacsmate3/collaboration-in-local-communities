import { ChatPageClient } from "./page.client"

interface ChatPageProps {
  params: Promise<{ chatId: string }>
}

export default async function ChatPage({ params }: ChatPageProps) {
  const { chatId } = await params
  return <ChatPageClient chatId={chatId} />
}
