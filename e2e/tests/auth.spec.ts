import { test, expect, type Page } from "@playwright/test"

const SEEDED_USER = {
  email: "user@local.test",
  password: "User123!",
} as const

const FEED_PATH = "/feed"

function uniqueEmail(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@local.test`
}

async function expectOnFeed(page: Page): Promise<void> {
  await page.waitForURL(`**${FEED_PATH}`)
  await expect(
    page.getByRole("heading", { name: "What's happening locally" })
  ).toBeVisible()
}

test.describe("Authentication", () => {
  test("user can log in with seeded credentials and lands on the feed", async ({
    page,
  }) => {
    await page.goto("/login")

    // CardTitle renders a <div>, not a real heading, so we can't use
    // getByRole("heading") here. Match the visible title text instead.
    await expect(page.getByText("Welcome back", { exact: true })).toBeVisible()

    await page.getByLabel("Email").fill(SEEDED_USER.email)
    await page.getByLabel("Password").fill(SEEDED_USER.password)
    await page.getByRole("button", { name: "Sign in" }).click()

    await expectOnFeed(page)
  })

  test("new user can register and is prompted to verify their email", async ({
    page,
  }) => {
    const email = uniqueEmail("e2e-register")
    const password = "E2eTest123!"
    const displayName = "E2E Tester"
    const workplace = "Helpful Neighbours Co-op"
    const role = "Community member"
    const location = "Budapest, Hungary"
    const bio =
      "Automated end-to-end test account that loves helping neighbours."

    await page.goto("/register")

    await expect(
      page.getByText("Create your account", { exact: true })
    ).toBeVisible()

    // Account step. "Password" is matched exactly so it doesn't also resolve the
    // "Confirm password" field.
    await page.getByLabel("Email").fill(email)
    await page.getByLabel("Password", { exact: true }).fill(password)
    await page.getByLabel("Confirm password").fill(password)
    await page.getByRole("checkbox", { name: /I agree to the Terms/i }).check()
    await page.getByRole("button", { name: "Continue" }).click()

    // Profile step — fill the full profile, not just the required name.
    // Optional fields render their label with a trailing "(optional)", so these
    // use substring (non-exact) label matching.
    await expect(page.getByLabel("Full name")).toBeVisible()
    await page.getByLabel("Full name").fill(displayName)
    await page.getByLabel("Workplace / school").fill(workplace)
    await page.getByLabel("Role").fill(role)
    // Free-text location (no geocoding call) — both lat/long stay unset, which the
    // form accepts.
    await page.getByLabel("Location").fill(location)
    await page.getByLabel("Short bio").fill(bio)
    await page.getByRole("button", { name: "Create account" }).click()

    // Registration no longer signs the user in: the backend sends a verification
    // email and the UI shows a "Check your email" confirmation instead of the feed.
    await expect(
      page.getByText("Check your email", { exact: true })
    ).toBeVisible()
    await expect(
      page.getByRole("link", { name: "Back to sign in" })
    ).toBeVisible()
  })
})
