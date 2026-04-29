import { HugeiconsIcon } from "@hugeicons/react"
import {
  Briefcase01Icon,
  Location01Icon,
  ShieldUserIcon,
} from "@hugeicons/core-free-icons"

import { Badge } from "@/components/ui/badge"
import { UserAvatar } from "@/components/shared/user-avatar"
import type { User } from "@/lib/types"

interface ProfileHeaderProps {
  user: User
  /** Right-side actions (Edit profile / Message). */
  actions?: React.ReactNode
}

/**
 * Card-less identity header for profile pages.
 *
 * Splits stats into a sibling component (`ReputationCard`) so this stays
 * focused on identity + verification status.
 */
export function ProfileHeader({ user, actions }: ProfileHeaderProps) {
  return (
    <section className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-4">
        <UserAvatar size="lg" name={user.name} src={user.avatarUrl} />
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <h1 className="text-xl leading-tight font-semibold">{user.name}</h1>
            {user.verified ? (
              <Badge variant="success" className="gap-1">
                <HugeiconsIcon icon={ShieldUserIcon} />
                Verified
              </Badge>
            ) : null}
          </div>

          {user.bio ? (
            <p className="max-w-prose text-sm text-muted-foreground">
              {user.bio}
            </p>
          ) : null}

          <ul className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
            {user.workplace || user.position ? (
              <li className="flex items-center gap-1">
                <HugeiconsIcon icon={Briefcase01Icon} className="size-3.5" />
                {[user.position, user.workplace].filter(Boolean).join(" · ")}
              </li>
            ) : null}
            {user.location ? (
              <li className="flex items-center gap-1">
                <HugeiconsIcon icon={Location01Icon} className="size-3.5" />
                {user.location}
              </li>
            ) : null}
          </ul>

          {user.skills.length > 0 ? (
            <ul className="mt-1 flex flex-wrap gap-1.5">
              {user.skills.map((skill) => (
                <li key={skill}>
                  <Badge variant="muted">{skill}</Badge>
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      </div>
      {actions ? <div className="flex gap-2">{actions}</div> : null}
    </section>
  )
}
