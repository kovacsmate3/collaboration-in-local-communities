"use client"

import * as React from "react"
import * as signalR from "@microsoft/signalr"
import { useQueryClient } from "@tanstack/react-query"

import { conversationKeys } from "@/lib/api/conversations"
import { refreshAccessToken } from "@/lib/auth/token-bridge"

async function fetchToken(): Promise<string> {
  let res = await fetch("/api/auth/token")
  if (res.status === 401) {
    const refreshed = await refreshAccessToken()
    if (refreshed) {
      res = await fetch("/api/auth/token")
    }
  }
  if (!res.ok) return ""
  const data = (await res.json()) as { token: string }
  return data.token
}

export function useNotificationHub(profileId: string | undefined) {
  const qc = useQueryClient()

  React.useEffect(() => {
    if (!profileId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat", { accessTokenFactory: fetchToken })
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
