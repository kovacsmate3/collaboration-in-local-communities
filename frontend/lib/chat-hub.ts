"use client"

import * as React from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient, type InfiniteData } from "@tanstack/react-query"

import { conversationKeys } from "@/lib/api/conversations"
import type {
  ApiConversationPreview,
  ApiMessage,
  MessagesPage,
} from "@/lib/api/conversations"
import { fetchSignalRToken } from "@/lib/signalr-token"

export function useConversationHub(
  conversationId: string,
  currentProfileId: string | undefined
) {
  const qc = useQueryClient()

  React.useEffect(() => {
    if (!conversationId || !currentProfileId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat", {
        accessTokenFactory: fetchSignalRToken,
      })
      .withAutomaticReconnect()
      .build()

    connection.on("ReceiveMessage", (msg: Omit<ApiMessage, "isMine">) => {
      const message: ApiMessage = {
        ...msg,
        isMine: msg.senderProfileId === currentProfileId,
      }
      qc.setQueryData<InfiniteData<MessagesPage>>(
        conversationKeys.messages(conversationId),
        (prev) => {
          if (!prev) {
            return {
              pages: [{ messages: [message], hasMore: false }],
              pageParams: [undefined],
            }
          }
          const lastPage = prev.pages[prev.pages.length - 1]
          if (lastPage.messages.some((m) => m.id === message.id)) return prev
          return {
            ...prev,
            pages: [
              ...prev.pages.slice(0, -1),
              { ...lastPage, messages: [...lastPage.messages, message] },
            ],
          }
        }
      )

      // Patch the inbox preview directly so the "last message" under each
      // conversation updates instantly, instead of waiting for an invalidate
      // → refetch round-trip (which is what made the preview lag in practice).
      qc.setQueryData<ApiConversationPreview[]>(conversationKeys.list, (prev) =>
        prev?.map((c) =>
          c.id === conversationId
            ? {
                ...c,
                lastMessageContent: message.content,
                lastMessageAt: message.sentAt,
                hasUnread: c.hasUnread || !message.isMine,
              }
            : c
        )
      )
    })

    connection.onreconnected(() => {
      void connection.invoke("JoinConversationAsync", conversationId)
      void qc.invalidateQueries({
        queryKey: conversationKeys.messages(conversationId),
      })
    })

    void connection
      .start()
      .then(() => connection.invoke("JoinConversationAsync", conversationId))
      .catch((err: unknown) => {
        console.warn("SignalR connection failed", err)
      })

    return () => {
      void connection.stop()
    }
  }, [conversationId, currentProfileId, qc])
}
