"use client"

import * as React from "react"
import Link from "next/link"
import { usePathname, useRouter, useSearchParams } from "next/navigation"

import { Button } from "@/components/ui/button"
import { PageHeader } from "@/components/shared/page-header"
import { TaskList } from "@/components/tasks/task-list"
import {
  DEFAULT_FILTERS,
  RADIUS_OPTIONS,
  TaskFilters,
  type RadiusOptionValue,
  type TaskFiltersState,
} from "@/components/tasks/task-filters"
import { useCategories } from "@/lib/api/categories"
import { useOwnProfile } from "@/lib/api/profile"
import { useInfiniteTaskList } from "@/lib/api/tasks"
import type { ApiTask, TaskListFilters } from "@/lib/api/tasks"
import { RECENCY_OPTIONS } from "@/lib/constants"

type RecencyValue = (typeof RECENCY_OPTIONS)[number]["value"]

/**
 * Canonical task-discovery feed. Replaces the former Seeker (#37) and
 * Helper (#39) feeds: one route, one set of filters, role-neutral copy.
 *
 * Server-side filtering (category, compensation, recency, radius) is
 * forwarded to `GET /api/tasks` so that pagination operates on the
 * already-filtered result set. The free-text query is the only filter
 * still applied client-side on top of the fetched pages. Filter state
 * is mirrored in the URL so refreshes and shares preserve the view.
 */
export function FeedPageClient() {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()

  const { data: categoriesData = [] } = useCategories()
  const { data: profile } = useOwnProfile()

  const origin = React.useMemo(() => {
    if (
      profile &&
      typeof profile.latitude === "number" &&
      Number.isFinite(profile.latitude) &&
      typeof profile.longitude === "number" &&
      Number.isFinite(profile.longitude)
    ) {
      return { latitude: profile.latitude, longitude: profile.longitude }
    }
    return null
  }, [profile])
  const hasOrigin = origin !== null

  // ── Filter state, seeded from the URL ──────────────────────────────────────
  const filters = React.useMemo<TaskFiltersState>(
    () => readFiltersFromUrl(searchParams, categoriesData),
    [searchParams, categoriesData]
  )

  // When the user picks the radius dropdown without having a stored profile
  // location, snap it back to "any" so we never send a half-built proximity
  // triplet to the backend.
  const effectiveRadius: RadiusOptionValue = hasOrigin ? filters.radius : "any"

  const handleFiltersChange = React.useCallback(
    (next: TaskFiltersState) => {
      const params = serializeFilters(next)
      const qs = params.toString()
      const url = qs.length > 0 ? pathname + "?" + qs : pathname
      router.replace(url, { scroll: false })
    },
    [pathname, router]
  )

  // ── Server-side filters ────────────────────────────────────────────────────
  const radiusMeters = React.useMemo(
    () =>
      RADIUS_OPTIONS.find((opt) => opt.value === effectiveRadius)?.meters ??
      null,
    [effectiveRadius]
  )

  const serverFilters = React.useMemo<TaskListFilters>(() => {
    const base: TaskListFilters = { status: "Open" }
    if (filters.category !== "all") {
      base.categoryId = filters.category
    }
    if (filters.compensation !== "all") {
      base.compensationType = filters.compensation
    }
    const recencyCutoff = recencyToIsoCutoff(filters.recency)
    if (recencyCutoff) {
      base.createdAfter = recencyCutoff
    }
    if (origin && typeof radiusMeters === "number") {
      base.latitude = origin.latitude
      base.longitude = origin.longitude
      base.radiusMeters = radiusMeters
    }
    return base
  }, [
    filters.category,
    filters.compensation,
    filters.recency,
    origin,
    radiusMeters,
  ])

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
    isError,
  } = useInfiniteTaskList(serverFilters)

  const rawTasks = React.useMemo(() => data?.pages.flat() ?? [], [data])

  // ── Client-side filters layered on top of the server response ──────────────
  // Only the free-text query is filtered client-side; all other dimensions are
  // forwarded to the backend so that infinite-scroll pages are real pages of
  // already-filtered results.
  const tasks = React.useMemo(
    () => applyClientFilters(rawTasks, filters),
    [rawTasks, filters]
  )

  // ── Infinite scroll sentinel ───────────────────────────────────────────────
  const sentinelRef = React.useRef<HTMLDivElement | null>(null)
  React.useEffect(() => {
    const sentinel = sentinelRef.current
    if (!sentinel || !hasNextPage) return

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry?.isIntersecting && !isFetchingNextPage) {
          void fetchNextPage()
        }
      },
      { rootMargin: "400px 0px" }
    )

    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [fetchNextPage, hasNextPage, isFetchingNextPage])

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Local tasks"
        description="Open requests in your community. Need a hand? Post a task in seconds."
        actions={
          <Button asChild>
            <Link href="/post-task">Post a task</Link>
          </Button>
        }
      />

      <TaskFilters
        value={filters}
        onChange={handleFiltersChange}
        categories={categoriesData}
        hasOrigin={hasOrigin}
      />

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading tasks…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">
          Could not load tasks. Please try again.
        </p>
      ) : (
        <>
          <TaskList
            tasks={tasks}
            emptyTitle={
              hasActiveFilters(filters)
                ? "No tasks match your filters"
                : "No open requests right now"
            }
            emptyDescription={
              hasActiveFilters(filters)
                ? "Try widening the category, reward, distance, or recency range."
                : "Be the first to post one — your neighbours might be ready to help."
            }
            hideStatus
            layout="grid"
          />
          {/*
           * Show pagination controls whenever the backend reports more pages,
           * even if the current visible list is empty after applying the
           * client-side search. A filtered first page can legitimately be
           * empty while later pages still contain matching results.
           */}
          {tasks.length > 0 || hasNextPage ? (
            <div ref={sentinelRef} className="flex justify-center py-2">
              {isFetchingNextPage ? (
                <p className="text-sm text-muted-foreground">
                  Loading more tasks…
                </p>
              ) : hasNextPage ? (
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => void fetchNextPage()}
                >
                  Load more
                </Button>
              ) : (
                <p className="text-sm text-muted-foreground">
                  You are all caught up.
                </p>
              )}
            </div>
          ) : null}
        </>
      )}
    </div>
  )
}

// ── URL <-> filter state helpers ─────────────────────────────────────────────

const SEARCH_PARAM_KEYS = {
  query: "q",
  category: "category",
  compensation: "compensation",
  recency: "recency",
  radius: "radius",
} as const

interface ReadableSearchParams {
  get(name: string): string | null
}

function readFiltersFromUrl(
  searchParams: ReadableSearchParams,
  categories: { id: string }[]
): TaskFiltersState {
  const query = (searchParams.get(SEARCH_PARAM_KEYS.query) ?? "").trim()

  const rawCategory = searchParams.get(SEARCH_PARAM_KEYS.category) ?? "all"
  const category =
    rawCategory === "all" || categories.some((c) => c.id === rawCategory)
      ? rawCategory
      : "all"

  const rawCompensation =
    searchParams.get(SEARCH_PARAM_KEYS.compensation) ?? "all"
  const compensation: TaskFiltersState["compensation"] =
    rawCompensation === "paid" ||
    rawCompensation === "points" ||
    rawCompensation === "barter"
      ? rawCompensation
      : rawCompensation === "voluntary"
        ? "points"
        : "all"

  const rawRecency = searchParams.get(SEARCH_PARAM_KEYS.recency) ?? "any"
  const recency: RecencyValue = RECENCY_OPTIONS.some(
    (o) => o.value === rawRecency
  )
    ? (rawRecency as RecencyValue)
    : "any"

  const rawRadius = searchParams.get(SEARCH_PARAM_KEYS.radius) ?? "any"
  const radius: RadiusOptionValue = RADIUS_OPTIONS.some(
    (o) => o.value === rawRadius
  )
    ? (rawRadius as RadiusOptionValue)
    : "any"

  return { query, category, compensation, recency, radius }
}

function serializeFilters(filters: TaskFiltersState): URLSearchParams {
  const params = new URLSearchParams()
  const query = filters.query.trim()
  if (query) params.set(SEARCH_PARAM_KEYS.query, query)
  if (filters.category !== DEFAULT_FILTERS.category) {
    params.set(SEARCH_PARAM_KEYS.category, filters.category)
  }
  if (filters.compensation !== DEFAULT_FILTERS.compensation) {
    params.set(SEARCH_PARAM_KEYS.compensation, filters.compensation)
  }
  if (filters.recency !== DEFAULT_FILTERS.recency) {
    params.set(SEARCH_PARAM_KEYS.recency, filters.recency)
  }
  if (filters.radius !== DEFAULT_FILTERS.radius) {
    params.set(SEARCH_PARAM_KEYS.radius, filters.radius)
  }
  return params
}

function hasActiveFilters(filters: TaskFiltersState): boolean {
  return (
    filters.query !== DEFAULT_FILTERS.query ||
    filters.category !== DEFAULT_FILTERS.category ||
    filters.compensation !== DEFAULT_FILTERS.compensation ||
    filters.recency !== DEFAULT_FILTERS.recency ||
    filters.radius !== DEFAULT_FILTERS.radius
  )
}

// ── Server-side filter translation ───────────────────────────────────────────

const RECENCY_CUTOFF_MS: Record<RecencyValue, number | null> = {
  any: null,
  "24h": 24 * 60 * 60 * 1000,
  "7d": 7 * 24 * 60 * 60 * 1000,
  "30d": 30 * 24 * 60 * 60 * 1000,
}

/**
 * Translate the recency bucket into an ISO timestamp the backend can use as
 * the `createdAfter` cutoff. Returns `null` for "any" so the filter is
 * omitted entirely.
 */
function recencyToIsoCutoff(recency: RecencyValue): string | null {
  const ms = RECENCY_CUTOFF_MS[recency]
  if (ms === null) return null
  return new Date(Date.now() - ms).toISOString()
}

// ── Client-side post-fetch filtering ─────────────────────────────────────────

function applyClientFilters(
  tasks: ApiTask[],
  filters: TaskFiltersState
): ApiTask[] {
  const q = filters.query.trim().toLowerCase()
  if (!q) return tasks

  return tasks.filter((t) => {
    const haystack =
      `${t.title} ${t.description} ${t.locationText ?? ""}`.toLowerCase()
    return haystack.includes(q)
  })
}
