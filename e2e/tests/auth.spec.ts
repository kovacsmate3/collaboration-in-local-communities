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

  test("new user can register and is signed in on the feed", async ({
    page,
  }) => {
    const email = uniqueEmail("e2e-register")
    const password = "E2eTest123!"
    const displayName = "E2E Tester"

    await page.goto("/register")

    await expect(
      page.getByText("Create your account", { exact: true })
    ).toBeVisible()

    // Account step
    await page.getByLabel("Email").fill(email)
    await page.getByLabel("Password").fill(password)
    await page.getByRole("checkbox").check()
    await page.getByRole("button", { name: "Continue" }).click()

    // Profile step
    await expect(page.getByLabel("Full name")).toBeVisible()
    await page.getByLabel("Full name").fill(displayName)
    await page.getByRole("button", { name: "Create account" }).click()

    await expectOnFeed(page)
  })
})
