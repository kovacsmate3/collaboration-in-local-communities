"use client"

import { useTranslations } from "next-intl"

import { PageHeader } from "@/components/shared/page-header"
import { ChatList } from "@/components/messages/chat-list"
import { ErrorState } from "@/components/shared/error-state"
import { LoadingState } from "@/components/shared/loading-state"
import { Card } from "@/components/ui/card"
import { useConversations } from "@/lib/api/conversations"

export function MessagesPageClient() {
  const t = useTranslations("messages.page")
  const {
    data: conversations = [],
    isLoading,
    isError,
    refetch,
  } = useConversations()

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title={t("title")} description={t("description")} />
      {isLoading ? (
        <LoadingState rows={3} />
      ) : isError ? (
        <ErrorState onRetry={() => void refetch()} />
      ) : (
        <Card className="overflow-hidden p-0">
          <ChatList conversations={conversations} />
        </Card>
      )}
    </div>
  )
}
