export interface ColorToken {
  name: string
  cssVariable: string
  backgroundClass: string
  foregroundClass: string
  usage: string
}

export interface TokenGroup<TToken> {
  title: string
  description: string
  tokens: readonly TToken[]
}

export interface FontToken {
  name: string
  cssVariable: string
  className: string
  usage: string
  sample: string
}

export interface TypographyToken {
  name: string
  cssVariable: string
  className: string
  usage: string
  sample: string
}

export interface RadiusToken {
  name: string
  cssVariable: string
  className: string
  usage: string
}

export interface ShadowToken {
  name: string
  cssVariable: string
  className: string
  usage: string
}

export const colorTokenGroups = [
  {
    title: "Core Surfaces",
    description: "Page, card, popover, form, and focus surfaces.",
    tokens: [
      {
        name: "background",
        cssVariable: "--background",
        backgroundClass: "bg-background",
        foregroundClass: "text-foreground",
        usage: "Application canvas.",
      },
      {
        name: "foreground",
        cssVariable: "--foreground",
        backgroundClass: "bg-foreground",
        foregroundClass: "text-background",
        usage: "Primary text and high-emphasis icons.",
      },
      {
        name: "card",
        cssVariable: "--card",
        backgroundClass: "bg-card",
        foregroundClass: "text-card-foreground",
        usage: "Repeated content panels and compact data containers.",
      },
      {
        name: "card-foreground",
        cssVariable: "--card-foreground",
        backgroundClass: "bg-card-foreground",
        foregroundClass: "text-card",
        usage: "Text placed on card surfaces.",
      },
      {
        name: "popover",
        cssVariable: "--popover",
        backgroundClass: "bg-popover",
        foregroundClass: "text-popover-foreground",
        usage: "Menus, selects, dialogs, and transient overlays.",
      },
      {
        name: "popover-foreground",
        cssVariable: "--popover-foreground",
        backgroundClass: "bg-popover-foreground",
        foregroundClass: "text-popover",
        usage: "Text placed on popover surfaces.",
      },
      {
        name: "border",
        cssVariable: "--border",
        backgroundClass: "bg-border",
        foregroundClass: "text-foreground",
        usage: "Dividers and structural borders.",
      },
      {
        name: "input",
        cssVariable: "--input",
        backgroundClass: "bg-input",
        foregroundClass: "text-foreground",
        usage: "Input borders and low-emphasis form fills.",
      },
      {
        name: "ring",
        cssVariable: "--ring",
        backgroundClass: "bg-ring",
        foregroundClass: "text-background",
        usage: "Keyboard focus ring color.",
      },
    ],
  },
  {
    title: "Actions",
    description: "Interactive intent colors used by shadcn/ui components.",
    tokens: [
      {
        name: "primary",
        cssVariable: "--primary",
        backgroundClass: "bg-primary",
        foregroundClass: "text-primary-foreground",
        usage: "Primary calls to action and active navigation.",
      },
      {
        name: "primary-foreground",
        cssVariable: "--primary-foreground",
        backgroundClass: "bg-primary-foreground",
        foregroundClass: "text-primary",
        usage: "Text and icons on primary surfaces.",
      },
      {
        name: "secondary",
        cssVariable: "--secondary",
        backgroundClass: "bg-secondary",
        foregroundClass: "text-secondary-foreground",
        usage: "Secondary buttons, neutral badges, and subtle fills.",
      },
      {
        name: "secondary-foreground",
        cssVariable: "--secondary-foreground",
        backgroundClass: "bg-secondary-foreground",
        foregroundClass: "text-secondary",
        usage: "Text and icons on secondary surfaces.",
      },
      {
        name: "accent",
        cssVariable: "--accent",
        backgroundClass: "bg-accent",
        foregroundClass: "text-accent-foreground",
        usage: "Hover and selected row states.",
      },
      {
        name: "accent-foreground",
        cssVariable: "--accent-foreground",
        backgroundClass: "bg-accent-foreground",
        foregroundClass: "text-accent",
        usage: "Text and icons on accent surfaces.",
      },
      {
        name: "destructive",
        cssVariable: "--destructive",
        backgroundClass: "bg-destructive",
        foregroundClass: "text-background",
        usage: "Deletion, cancellation, and irreversible actions.",
      },
    ],
  },
  {
    title: "Feedback",
    description: "Status colors for validation, alerts, and local-task state.",
    tokens: [
      {
        name: "success",
        cssVariable: "--success",
        backgroundClass: "bg-success",
        foregroundClass: "text-success-foreground",
        usage: "Completed work, paid compensation, and positive outcomes.",
      },
      {
        name: "success-foreground",
        cssVariable: "--success-foreground",
        backgroundClass: "bg-success-foreground",
        foregroundClass: "text-success",
        usage: "Text and icons on success surfaces.",
      },
      {
        name: "warning",
        cssVariable: "--warning",
        backgroundClass: "bg-warning",
        foregroundClass: "text-warning-foreground",
        usage: "Attention states and barter compensation.",
      },
      {
        name: "warning-foreground",
        cssVariable: "--warning-foreground",
        backgroundClass: "bg-warning-foreground",
        foregroundClass: "text-warning",
        usage: "Text and icons on warning surfaces.",
      },
      {
        name: "info",
        cssVariable: "--info",
        backgroundClass: "bg-info",
        foregroundClass: "text-info-foreground",
        usage: "Informational state, help copy, and credit-like exchange.",
      },
      {
        name: "info-foreground",
        cssVariable: "--info-foreground",
        backgroundClass: "bg-info-foreground",
        foregroundClass: "text-info",
        usage: "Text and icons on info surfaces.",
      },
      {
        name: "reputation",
        cssVariable: "--reputation",
        backgroundClass: "bg-reputation",
        foregroundClass: "text-reputation-foreground",
        usage: "Ratings, trust cues, and reputation summaries.",
      },
      {
        name: "reputation-foreground",
        cssVariable: "--reputation-foreground",
        backgroundClass: "bg-reputation-foreground",
        foregroundClass: "text-reputation",
        usage: "Text and icons on reputation surfaces.",
      },
    ],
  },
  {
    title: "Domain",
    description: "Product-specific tokens that keep local-task concepts named.",
    tokens: [
      {
        name: "compensation-paid",
        cssVariable: "--compensation-paid",
        backgroundClass: "bg-compensation-paid",
        foregroundClass: "text-success-foreground",
        usage: "Paid task compensation.",
      },
      {
        name: "compensation-credit",
        cssVariable: "--compensation-credit",
        backgroundClass: "bg-compensation-credit",
        foregroundClass: "text-info-foreground",
        usage: "Credit, points, or non-cash ledger compensation.",
      },
      {
        name: "compensation-barter",
        cssVariable: "--compensation-barter",
        backgroundClass: "bg-compensation-barter",
        foregroundClass: "text-warning-foreground",
        usage: "Barter exchange compensation.",
      },
    ],
  },
  {
    title: "Charts",
    description: "Ordered data-visualization palette for dashboard charts.",
    tokens: [
      {
        name: "chart-1",
        cssVariable: "--chart-1",
        backgroundClass: "bg-chart-1",
        foregroundClass: "text-foreground",
        usage: "First chart series.",
      },
      {
        name: "chart-2",
        cssVariable: "--chart-2",
        backgroundClass: "bg-chart-2",
        foregroundClass: "text-foreground",
        usage: "Second chart series.",
      },
      {
        name: "chart-3",
        cssVariable: "--chart-3",
        backgroundClass: "bg-chart-3",
        foregroundClass: "text-background",
        usage: "Third chart series.",
      },
      {
        name: "chart-4",
        cssVariable: "--chart-4",
        backgroundClass: "bg-chart-4",
        foregroundClass: "text-background",
        usage: "Fourth chart series.",
      },
      {
        name: "chart-5",
        cssVariable: "--chart-5",
        backgroundClass: "bg-chart-5",
        foregroundClass: "text-background",
        usage: "Fifth chart series.",
      },
    ],
  },
  {
    title: "Sidebar",
    description: "Navigation shell palette for the admin and app sidebars.",
    tokens: [
      {
        name: "sidebar",
        cssVariable: "--sidebar",
        backgroundClass: "bg-sidebar",
        foregroundClass: "text-sidebar-foreground",
        usage: "Sidebar background.",
      },
      {
        name: "sidebar-foreground",
        cssVariable: "--sidebar-foreground",
        backgroundClass: "bg-sidebar-foreground",
        foregroundClass: "text-sidebar",
        usage: "Sidebar text and icons.",
      },
      {
        name: "sidebar-primary",
        cssVariable: "--sidebar-primary",
        backgroundClass: "bg-sidebar-primary",
        foregroundClass: "text-sidebar-primary-foreground",
        usage: "Active sidebar item.",
      },
      {
        name: "sidebar-primary-foreground",
        cssVariable: "--sidebar-primary-foreground",
        backgroundClass: "bg-sidebar-primary-foreground",
        foregroundClass: "text-sidebar-primary",
        usage: "Text and icons on active sidebar items.",
      },
      {
        name: "sidebar-accent",
        cssVariable: "--sidebar-accent",
        backgroundClass: "bg-sidebar-accent",
        foregroundClass: "text-sidebar-accent-foreground",
        usage: "Sidebar hover and subtle selected states.",
      },
      {
        name: "sidebar-accent-foreground",
        cssVariable: "--sidebar-accent-foreground",
        backgroundClass: "bg-sidebar-accent-foreground",
        foregroundClass: "text-sidebar-accent",
        usage: "Text and icons on sidebar accent surfaces.",
      },
      {
        name: "sidebar-border",
        cssVariable: "--sidebar-border",
        backgroundClass: "bg-sidebar-border",
        foregroundClass: "text-foreground",
        usage: "Sidebar dividers.",
      },
      {
        name: "sidebar-ring",
        cssVariable: "--sidebar-ring",
        backgroundClass: "bg-sidebar-ring",
        foregroundClass: "text-background",
        usage: "Sidebar focus rings.",
      },
    ],
  },
] satisfies readonly TokenGroup<ColorToken>[]

export const fontTokens = [
  {
    name: "heading",
    cssVariable: "--font-heading",
    className: "font-heading",
    usage: "Page titles, section headings, and card titles.",
    sample: "Structured trust for local help",
  },
  {
    name: "sans",
    cssVariable: "--font-sans",
    className: "font-sans",
    usage: "Default application UI text.",
    sample: "Neighbors can coordinate tasks with clear expectations.",
  },
  {
    name: "mono",
    cssVariable: "--font-mono",
    className: "font-mono",
    usage: "IDs, slugs, API paths, and audit values.",
    sample: "task_8f2a9c",
  },
] satisfies readonly FontToken[]

export const typographyTokens = [
  {
    name: "display",
    cssVariable: "--text-display",
    className: "font-heading text-display",
    usage: "First-viewport marketing or product headlines.",
    sample: "Local help, structured trust.",
  },
  {
    name: "page-title",
    cssVariable: "--text-page-title",
    className: "font-heading text-page-title",
    usage: "Main page titles inside the application shell.",
    sample: "Available tasks nearby",
  },
  {
    name: "section-title",
    cssVariable: "--text-section-title",
    className: "font-heading text-section-title",
    usage: "Panel headings and form section titles.",
    sample: "Compensation details",
  },
  {
    name: "body",
    cssVariable: "--text-body",
    className: "text-body",
    usage: "Longer explanatory copy and readable descriptions.",
    sample: "Use this task summary to decide whether the fit is right.",
  },
  {
    name: "body-sm",
    cssVariable: "--text-body-sm",
    className: "text-body-sm",
    usage: "Dense interface text in cards, tables, and forms.",
    sample: "Posted 12 minutes ago in District XI.",
  },
  {
    name: "caption",
    cssVariable: "--text-caption",
    className: "text-caption",
    usage: "Metadata, labels, helper text, and compact badges.",
    sample: "Verified helper",
  },
] satisfies readonly TypographyToken[]

export const radiusTokens = [
  {
    name: "sm",
    cssVariable: "--radius-sm",
    className: "rounded-sm",
    usage: "Small controls and inner elements.",
  },
  {
    name: "md",
    cssVariable: "--radius-md",
    className: "rounded-md",
    usage: "Buttons, inputs, and compact menu items.",
  },
  {
    name: "lg",
    cssVariable: "--radius-lg",
    className: "rounded-lg",
    usage: "Tables, panels, and standard cards.",
  },
  {
    name: "xl",
    cssVariable: "--radius-xl",
    className: "rounded-xl",
    usage: "Large cards and modal surfaces.",
  },
  {
    name: "2xl",
    cssVariable: "--radius-2xl",
    className: "rounded-2xl",
    usage: "Large media or spacious feature panels.",
  },
  {
    name: "4xl",
    cssVariable: "--radius-4xl",
    className: "rounded-4xl",
    usage: "Pills and compact badges.",
  },
] satisfies readonly RadiusToken[]

export const shadowTokens = [
  {
    name: "card",
    cssVariable: "--shadow-card",
    className: "shadow-card",
    usage: "Subtle lift for repeated content surfaces.",
  },
  {
    name: "raised",
    cssVariable: "--shadow-raised",
    className: "shadow-raised",
    usage: "Floating overlays and elevated panels.",
  },
] satisfies readonly ShadowToken[]
