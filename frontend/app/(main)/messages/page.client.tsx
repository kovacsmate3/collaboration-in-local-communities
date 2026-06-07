"use client"

import { useTranslations } from "next-intl"

import { PageHeader } from "@/components/shared/page-header"
import { ChatList } from "@/components/messages/chat-list"
import { Card } from "@/components/ui/card"
import { useConversations } from "@/lib/api/conversations"

export function MessagesPageClient() {
  const t = useTranslations("messages.page")
  const { data: conversations = [], isLoading, isError } = useConversations()

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title={t("title")} description={t("description")} />
      {isLoading ? (
        <p className="text-sm text-muted-foreground">{t("loading")}</p>
      ) : isError ? (
        <p className="text-sm text-destructive">{t("loadError")}</p>
      ) : (
        <Card className="overflow-hidden p-0">
          <ChatList conversations={conversations} />
        </Card>
      )}
    </div>
  )
}
