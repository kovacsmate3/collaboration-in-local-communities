import type { Metadata } from "next"
import { notFound } from "next/navigation"
import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { ReputationCard } from "@/components/profile/reputation-card"
import { ReputationSummary } from "@/components/shared/reputation-summary"
import {
  colorTokenGroups,
  fontTokens,
  radiusTokens,
  shadowTokens,
  typographyTokens,
  type ColorToken,
  type FontToken,
  type RadiusToken,
  type ShadowToken,
  type TypographyToken,
} from "@/lib/design-tokens"
import { cn } from "@/lib/utils"

export const metadata: Metadata = {
  title: "Component Gallery",
  description: "Design tokens and reusable frontend component states.",
}

const buttonVariants = [
  "default",
  "secondary",
  "outline",
  "ghost",
  "destructive",
  "destructive-solid",
  "link",
] as const

const badgeVariants = [
  "default",
  "secondary",
  "success",
  "warning",
  "info",
  "destructive",
  "outline",
  "muted",
  "ghost",
  "link",
] as const

const singleReviewTrend = [
  {
    date: "2026-01-04",
    score: 10,
    averageRating: 0,
    reviewCount: 0,
    completedTaskCount: 1,
  },
  {
    date: "2026-01-08",
    score: 20,
    averageRating: 5,
    reviewCount: 1,
    completedTaskCount: 1,
  },
  {
    date: "2026-01-12",
    score: 50,
    averageRating: 5,
    reviewCount: 1,
    completedTaskCount: 4,
  },
] as const

const highVolumeTrend = [
  {
    date: "2026-02-01",
    score: 98,
    averageRating: 4.75,
    reviewCount: 4,
    completedTaskCount: 6,
  },
  {
    date: "2026-02-12",
    score: 235,
    averageRating: 4.8,
    reviewCount: 12,
    completedTaskCount: 12,
  },
  {
    date: "2026-03-02",
    score: 397,
    averageRating: 4.7,
    reviewCount: 21,
    completedTaskCount: 20,
  },
  {
    date: "2026-03-19",
    score: 584,
    averageRating: 4.9,
    reviewCount: 31,
    completedTaskCount: 28,
  },
] as const

export default function ComponentGalleryPage() {
  if (process.env.NODE_ENV === "production") {
    notFound()
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-10 px-4 py-8 sm:px-6 lg:px-8">
        <header className="flex flex-col gap-3 border-b border-border pb-6">
          <Badge variant="outline" className="w-fit">
            Sprint 1 foundation
          </Badge>
          <div className="flex max-w-3xl flex-col gap-2">
            <h1 className="font-heading text-display">Component Gallery</h1>
            <p className="text-body text-muted-foreground">
              Canonical rendering for the frontend palette, typography, radius,
              elevation, and reusable shadcn/ui component states.
            </p>
          </div>
        </header>

        <GallerySection
          title="Palette"
          description="Every semantic color token exposed through Tailwind utilities."
        >
          <div className="flex flex-col gap-8">
            {colorTokenGroups.map((group) => (
              <section key={group.title} className="flex flex-col gap-3">
                <div className="flex flex-col gap-1">
                  <h2 className="font-heading text-section-title">
                    {group.title}
                  </h2>
                  <p className="text-body-sm text-muted-foreground">
                    {group.description}
                  </p>
                </div>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                  {group.tokens.map((token) => (
                    <ColorTokenCard key={token.name} token={token} />
                  ))}
                </div>
              </section>
            ))}
          </div>
        </GallerySection>

        <GallerySection
          title="Typography"
          description="Font families and semantic type scale used across app surfaces."
        >
          <div className="grid gap-4 lg:grid-cols-[0.8fr_1.2fr]">
            <div className="grid gap-3">
              {fontTokens.map((token) => (
                <FontTokenCard key={token.name} token={token} />
              ))}
            </div>
            <div className="grid gap-3">
              {typographyTokens.map((token) => (
                <TypographyTokenCard key={token.name} token={token} />
              ))}
            </div>
          </div>
        </GallerySection>

        <GallerySection
          title="Shape And Elevation"
          description="Reusable radius and shadow tokens for controls, cards, and overlays."
        >
          <div className="grid gap-4 lg:grid-cols-2">
            <div className="grid gap-3 sm:grid-cols-2">
              {radiusTokens.map((token) => (
                <RadiusTokenCard key={token.name} token={token} />
              ))}
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              {shadowTokens.map((token) => (
                <ShadowTokenCard key={token.name} token={token} />
              ))}
            </div>
          </div>
        </GallerySection>

        <GallerySection
          title="Components"
          description="Core variants rendered in one place to catch token regressions early."
        >
          <div className="grid gap-6 lg:grid-cols-2">
            <ComponentPanel title="Buttons">
              <div className="flex flex-wrap items-center gap-2">
                {buttonVariants.map((variant) => (
                  <Button key={variant} variant={variant}>
                    {variant}
                  </Button>
                ))}
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <Button size="xs">Extra small</Button>
                <Button size="sm">Small</Button>
                <Button>Default</Button>
                <Button size="lg">Large</Button>
              </div>
            </ComponentPanel>

            <ComponentPanel title="Badges">
              <div className="flex flex-wrap items-center gap-2">
                {badgeVariants.map((variant) => (
                  <Badge key={variant} variant={variant}>
                    {variant}
                  </Badge>
                ))}
              </div>
            </ComponentPanel>

            <ComponentPanel title="Form Controls">
              <div className="grid gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="gallery-title">Task title</Label>
                  <Input
                    id="gallery-title"
                    defaultValue="Move a bookshelf upstairs"
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="gallery-description">Description</Label>
                  <Textarea
                    id="gallery-description"
                    defaultValue="Two people needed, elevator available."
                  />
                </div>
              </div>
            </ComponentPanel>

            <Card>
              <CardHeader>
                <CardTitle>Task Card</CardTitle>
                <CardDescription>
                  Standard content surface with action footer.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex flex-wrap gap-2">
                  <Badge variant="success">Paid</Badge>
                  <Badge variant="warning">Barter</Badge>
                  <Badge variant="info">Credit</Badge>
                </div>
                <p className="text-body-sm text-muted-foreground">
                  Uses card, badge, typography, radius, and elevation tokens
                  without hard-coded palette classes.
                </p>
              </CardContent>
              <CardFooter className="gap-2">
                <Button size="sm">Offer help</Button>
                <Button size="sm" variant="outline">
                  Details
                </Button>
              </CardFooter>
            </Card>
          </div>
        </GallerySection>

        <GallerySection
          title="Reputation"
          description="Profile reputation widget and the inline summary primitive that other surfaces compose."
        >
          <div className="grid gap-6 lg:grid-cols-2">
            <ComponentPanel title="Inline summary (sm)">
              <div className="flex flex-col gap-3">
                <ReputationSummary
                  score={0}
                  averageRating={0}
                  reviewCount={0}
                  completedTaskCount={0}
                />
                <ReputationSummary
                  score={40}
                  averageRating={5}
                  reviewCount={1}
                  completedTaskCount={3}
                />
                <ReputationSummary
                  score={584}
                  averageRating={4.9}
                  reviewCount={31}
                  completedTaskCount={28}
                />
                <ReputationSummary
                  score={584}
                  averageRating={4.9}
                  reviewCount={31}
                  completedTaskCount={28}
                  size="md"
                />
                <ReputationSummary
                  score={584}
                  averageRating={4.9}
                  reviewCount={31}
                  completedTaskCount={28}
                  showScore={false}
                />
              </div>
            </ComponentPanel>

            <ComponentPanel title="Dashboard widget - new user">
              <ReputationCard
                score={0}
                averageRating={0}
                reviewCount={0}
                completedTaskCount={0}
                showOwnerCtas
              />
            </ComponentPanel>

            <ComponentPanel title="Dashboard widget - single review">
              <ReputationCard
                score={50}
                averageRating={5}
                reviewCount={1}
                completedTaskCount={4}
                trend={singleReviewTrend}
              />
            </ComponentPanel>

            <ComponentPanel title="Dashboard widget - high volume helper">
              <ReputationCard
                score={584}
                averageRating={4.9}
                reviewCount={31}
                completedTaskCount={28}
                trend={highVolumeTrend}
              />
            </ComponentPanel>
          </div>
        </GallerySection>
      </div>
    </main>
  )
}

function GallerySection({
  title,
  description,
  children,
}: Readonly<{
  title: string
  description: string
  children: ReactNode
}>) {
  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <h2 className="font-heading text-page-title">{title}</h2>
        <p className="text-body-sm text-muted-foreground">{description}</p>
      </div>
      {children}
    </section>
  )
}

function ColorTokenCard({ token }: Readonly<{ token: ColorToken }>) {
  return (
    <article className="overflow-hidden rounded-lg border border-border bg-card shadow-card">
      <div
        className={cn(
          "flex min-h-28 flex-col justify-between p-3",
          token.backgroundClass,
          token.foregroundClass
        )}
      >
        <span className="text-caption uppercase">{token.name}</span>
        <span className="font-mono text-xs">{token.cssVariable}</span>
      </div>
      <div className="grid gap-1 p-3 text-body-sm">
        <code className="font-mono text-xs text-muted-foreground">
          {token.backgroundClass}
        </code>
        <p className="text-muted-foreground">{token.usage}</p>
      </div>
    </article>
  )
}

function FontTokenCard({ token }: Readonly<{ token: FontToken }>) {
  return (
    <article className="grid gap-3 rounded-lg border border-border bg-card p-4 shadow-card">
      <div className="flex items-center justify-between gap-3">
        <h3 className="font-heading text-section-title">{token.name}</h3>
        <code className="font-mono text-xs text-muted-foreground">
          {token.cssVariable}
        </code>
      </div>
      <p className={cn("text-body", token.className)}>{token.sample}</p>
      <p className="text-body-sm text-muted-foreground">{token.usage}</p>
    </article>
  )
}

function TypographyTokenCard({ token }: Readonly<{ token: TypographyToken }>) {
  return (
    <article className="grid gap-3 rounded-lg border border-border bg-card p-4 shadow-card">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h3 className="font-heading text-section-title">{token.name}</h3>
        <code className="font-mono text-xs text-muted-foreground">
          {token.cssVariable}
        </code>
      </div>
      <p className={token.className}>{token.sample}</p>
      <p className="text-body-sm text-muted-foreground">{token.usage}</p>
    </article>
  )
}

function RadiusTokenCard({ token }: Readonly<{ token: RadiusToken }>) {
  return (
    <article className="grid gap-3 rounded-lg border border-border bg-card p-4 shadow-card">
      <div
        className={cn(
          "h-20 border border-primary bg-primary/10",
          token.className
        )}
      />
      <div className="grid gap-1 text-body-sm">
        <h3 className="font-medium">{token.name}</h3>
        <code className="font-mono text-xs text-muted-foreground">
          {token.cssVariable}
        </code>
        <p className="text-muted-foreground">{token.usage}</p>
      </div>
    </article>
  )
}

function ShadowTokenCard({ token }: Readonly<{ token: ShadowToken }>) {
  return (
    <article className="grid gap-3 rounded-lg border border-border bg-card p-4 shadow-card">
      <div
        className={cn(
          "h-20 rounded-lg border border-border bg-card",
          token.className
        )}
      />
      <div className="grid gap-1 text-body-sm">
        <h3 className="font-medium">{token.name}</h3>
        <code className="font-mono text-xs text-muted-foreground">
          {token.cssVariable}
        </code>
        <p className="text-muted-foreground">{token.usage}</p>
      </div>
    </article>
  )
}

function ComponentPanel({
  title,
  children,
}: Readonly<{ title: string; children: ReactNode }>) {
  return (
    <section className="flex flex-col gap-4 rounded-lg border border-border bg-card p-4 shadow-card">
      <h3 className="font-heading text-section-title">{title}</h3>
      {children}
    </section>
  )
}
