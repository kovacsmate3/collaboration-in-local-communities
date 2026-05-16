"use client"

import * as React from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient } from "@tanstack/react-query"

import { conversationKeys } from "@/lib/api/conversations"
import type { ApiMessage } from "@/lib/api/conversations"
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
      qc.setQueryData<ApiMessage[]>(
        conversationKeys.messages(conversationId),
        (prev) => {
          if (!prev) return [message]
          if (prev.some((m) => m.id === message.id)) return prev
          return [...prev, message]
        }
      )
      void qc.invalidateQueries({ queryKey: conversationKeys.list })
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
