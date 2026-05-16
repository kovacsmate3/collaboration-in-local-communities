"use client"

import * as React from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient } from "@tanstack/react-query"

import { conversationKeys } from "@/lib/api/conversations"
import { fetchSignalRToken } from "@/lib/signalr-token"

export function useNotificationHub(profileId: string | undefined) {
  const qc = useQueryClient()

  React.useEffect(() => {
    if (!profileId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat", { accessTokenFactory: fetchSignalRToken })
      .withAutomaticReconnect()
      .build()

    connection.on("NewMessageNotification", () => {
      void qc.invalidateQueries({ queryKey: conversationKeys.list })
    })

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
