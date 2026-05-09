import { HugeiconsIcon } from "@hugeicons/react"
import {
  Briefcase01Icon,
  Location01Icon,
  ShieldUserIcon,
} from "@hugeicons/core-free-icons"

import { Badge } from "@/components/ui/badge"
import { UserAvatar } from "@/components/shared/user-avatar"

interface ProfileHeaderProps {
  displayName: string
  bio?: string | null
  workplace?: string | null
  position?: string | null
  locationText?: string | null
  photoUrl?: string | null
  isVerified?: boolean
  actions?: React.ReactNode
}

export function ProfileHeader({
  displayName,
  bio,
  workplace,
  position,
  locationText,
  photoUrl,
  isVerified,
  actions,
}: ProfileHeaderProps) {
  return (
    <section className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-4">
        <UserAvatar size="lg" name={displayName} src={photoUrl ?? undefined} />
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <h1 className="text-xl leading-tight font-semibold">
              {displayName}
            </h1>
            {isVerified ? (
              <Badge variant="success" className="gap-1">
                <HugeiconsIcon icon={ShieldUserIcon} />
                Verified
              </Badge>
            ) : null}
          </div>

          {bio ? (
            <p className="max-w-prose text-sm text-muted-foreground">{bio}</p>
          ) : null}

          <ul className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
            {position || workplace ? (
              <li className="flex items-center gap-1">
                <HugeiconsIcon icon={Briefcase01Icon} className="size-3.5" />
                {[position, workplace].filter(Boolean).join(" · ")}
              </li>
            ) : null}
            {locationText ? (
              <li className="flex items-center gap-1">
                <HugeiconsIcon icon={Location01Icon} className="size-3.5" />
                {locationText}
              </li>
            ) : null}
          </ul>
        </div>
      </div>
      {actions ? <div className="flex gap-2">{actions}</div> : null}
    </section>
  )
}
