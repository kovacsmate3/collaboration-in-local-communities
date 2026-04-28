import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { cn } from "@/lib/utils"

interface UserAvatarProps {
  name: string
  src?: string
  size?: "sm" | "md" | "lg"
  className?: string
}

const SIZES = {
  sm: "size-7 text-[0.65rem]",
  md: "size-9 text-xs",
  lg: "size-14 text-sm",
}

/**
 * Project-specific avatar with deterministic initials fallback.
 *
 * Wraps the primitive Avatar to centralize sizing and initial logic so we
 * don't repeat it across every consumer.
 */
export function UserAvatar({ name, src, size = "md", className }: UserAvatarProps) {
  const initials = name
    .split(/\s+/)
    .map((part) => part[0])
    .filter(Boolean)
    .slice(0, 2)
    .join("")
    .toUpperCase()

  return (
    <Avatar className={cn(SIZES[size], className)}>
      {src ? <AvatarImage src={src} alt={name} /> : null}
      <AvatarFallback>{initials || "?"}</AvatarFallback>
    </Avatar>
  )
}
