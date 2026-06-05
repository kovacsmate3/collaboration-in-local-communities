import type { Metadata } from "next"
import type { ReactNode } from "react"
import { NextIntlClientProvider } from "next-intl"
import { getLocale, getMessages, getTranslations } from "next-intl/server"

import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"
import { QueryProvider } from "@/components/providers/query-provider"
import { AuthProvider } from "@/lib/auth-context"
import { RegisterDraftProvider } from "@/lib/register-draft"
import { Toaster } from "@/components/ui/sonner"
import { APP_NAME } from "@/lib/constants"

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("common")
  return {
    title: {
      default: APP_NAME,
      template: `%s | ${APP_NAME}`,
    },
    description: t("tagline"),
  }
}

export default async function RootLayout({
  children,
}: Readonly<{
  children: ReactNode
}>) {
  // Resolve the active locale + load its catalog server-side so SSR markup
  // is rendered in the right language with no client-side flash.
  const locale = await getLocale()
  const messages = await getMessages()

  return (
    <html
      lang={locale}
      suppressHydrationWarning
      className="font-sans antialiased"
    >
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <ThemeProvider>
            <QueryProvider>
              <AuthProvider>
                <RegisterDraftProvider>
                  {children}
                  <Toaster />
                </RegisterDraftProvider>
              </AuthProvider>
            </QueryProvider>
          </ThemeProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  )
}
