import { Skeleton } from "@/components/ui/skeleton"

export function AdminGuardSkeleton() {
  return (
    <div className="flex h-svh flex-col gap-4 p-8">
      <Skeleton className="h-8 w-48" />
      <div className="flex flex-1 gap-4">
        <Skeleton className="h-full w-56" />
        <div className="flex-1 space-y-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      </div>
    </div>
  )
}
