import type { Metadata } from "next"
import { notFound } from "next/navigation"
import type * as React from "react"

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
  children: React.ReactNode
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
}: Readonly<{ title: string; children: React.ReactNode }>) {
  return (
    <section className="flex flex-col gap-4 rounded-lg border border-border bg-card p-4 shadow-card">
      <h3 className="font-heading text-section-title">{title}</h3>
      {children}
    </section>
  )
}
