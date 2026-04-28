"use client"

import * as React from "react"

const MOBILE_BREAKPOINT = 768

/**
 * Reactive `is mobile?` flag based on a media query.
 *
 * Returns `false` during SSR to avoid hydration mismatches; the value
 * is corrected on the first client effect.
 */
export function useIsMobile(): boolean {
  const [isMobile, setIsMobile] = React.useState(false)

  React.useEffect(() => {
    const mql = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`)
    const update = () => setIsMobile(mql.matches)
    update()
    mql.addEventListener("change", update)
    return () => mql.removeEventListener("change", update)
  }, [])

  return isMobile
}
