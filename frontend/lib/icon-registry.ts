// This list must stay in sync with `backend/Application/Categories/AllowedCategoryIcons.cs`
// (`Backend.Application.Categories.AllowedCategoryIcons`); drift between the two is a bug.
import {
  BabyBottleIcon,
  Bone01Icon,
  Briefcase01Icon,
  BubbleChatIcon,
  Calendar01Icon,
  DeliveryTruck01Icon,
  FavouriteIcon,
  HandHelpingIcon,
  MoreHorizontalCircle01Icon,
  Mortarboard02Icon,
  RunningShoesIcon,
  ShoppingBag03Icon,
  SparklesIcon,
  StarIcon,
  ToolboxIcon,
  Wrench01Icon,
} from "@hugeicons/core-free-icons"
import type { IconSvgElement } from "@hugeicons/react"

export const ICON_REGISTRY = {
  DeliveryTruck01Icon,
  Mortarboard02Icon,
  Wrench01Icon,
  ShoppingBag03Icon,
  Bone01Icon,
  SparklesIcon,
  RunningShoesIcon,
  MoreHorizontalCircle01Icon,
  Briefcase01Icon,
  BubbleChatIcon,
  HandHelpingIcon,
  FavouriteIcon,
  StarIcon,
  ToolboxIcon,
  Calendar01Icon,
  BabyBottleIcon,
} as const satisfies Record<string, IconSvgElement>

export type IconKey = keyof typeof ICON_REGISTRY

function isIconKey(name: string): name is IconKey {
  return Object.prototype.hasOwnProperty.call(ICON_REGISTRY, name)
}

export function getIconForKey(name: string): IconSvgElement {
  return isIconKey(name)
    ? ICON_REGISTRY[name]
    : ICON_REGISTRY.MoreHorizontalCircle01Icon
}
