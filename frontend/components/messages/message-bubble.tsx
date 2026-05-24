import { cn } from "@/lib/utils"
import type { ApiMessage } from "@/lib/api/conversations"

interface MessageBubbleProps {
  message: ApiMessage
}

export function MessageBubble({ message }: MessageBubbleProps) {
  return (
    <div
      className={cn("flex", message.isMine ? "justify-end" : "justify-start")}
    >
      <div
        className={cn(
          "max-w-[80%] rounded-2xl px-3.5 py-2 text-sm",
          message.isMine
            ? "rounded-br-sm bg-primary text-primary-foreground"
            : "rounded-bl-sm bg-muted text-foreground"
        )}
      >
        {message.content}
      </div>
    </div>
  )
}
