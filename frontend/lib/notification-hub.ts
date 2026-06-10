"use client"

import * as React from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient } from "@tanstack/react-query"

import { conversationKeys } from "@/lib/api/conversations"
import type { ApiConversationPreview } from "@/lib/api/conversations"
import { fetchSignalRToken } from "@/lib/signalr-token"

interface NewMessageNotification {
  conversationId: string
  senderDisplayName: string
  contentPreview: string
}

export function useNotificationHub(profileId: string | undefined) {
  const qc = useQueryClient()

  React.useEffect(() => {
    if (!profileId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat", { accessTokenFactory: fetchSignalRToken })
      .withAutomaticReconnect()
      .build()

    connection.on(
      "NewMessageNotification",
      (notification: NewMessageNotification) => {
        // Patch the inbox preview immediately for a known conversation so the
        // unread badge and last message update without waiting for a refetch.
        qc.setQueryData<ApiConversationPreview[]>(
          conversationKeys.list,
          (prev) =>
            prev?.map((c) =>
              c.id === notification.conversationId
                ? {
                    ...c,
                    lastMessageContent: notification.contentPreview,
                    lastMessageAt: new Date().toISOString(),
                    hasUnread: true,
                  }
                : c
            )
        )
        // Still invalidate so a brand-new conversation (e.g. a fresh task
        // application) that isn't in the cache yet gets fetched.
        void qc.invalidateQueries({ queryKey: conversationKeys.list })
      }
    )

    connection.onreconnected(() => {
      void connection.invoke("JoinUserGroupAsync")
      void qc.invalidateQueries({ queryKey: conversationKeys.list })
    })

    void connection
      .start()
      .then(() => connection.invoke("JoinUserGroupAsync"))
      .catch((err: unknown) => {
        console.warn("Notification hub connection failed", err)
      })

    return () => {
      void connection.stop()
    }
  }, [profileId, qc])
}
