import { test, expect, type Page } from "@playwright/test"

// Seeded development account — guaranteed by UserSeedingHelper to exist, be
// email-confirmed, and have a current-terms acceptance record on every backend
// start. That keeps this spec deterministic across CI and local runs.
const SEEDED_USER = {
  email: "user@local.test",
  password: "User123!",
} as const

function uniqueTaskTitle(prefix: string): string {
  return `${prefix} ${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`
}

async function loginAsSeededUser(page: Page): Promise<void> {
  await page.goto("/login")
  await expect(page.getByText("Welcome back", { exact: true })).toBeVisible()
  await page.getByLabel("Email").fill(SEEDED_USER.email)
  await page.getByLabel("Password", { exact: true }).fill(SEEDED_USER.password)
  await page.getByRole("button", { name: "Sign in" }).click()
  await page.waitForURL("**/feed")
  await expect(page.getByRole("heading", { name: "Local tasks" })).toBeVisible()
}

test.describe("Task discovery", () => {
  test("a newly posted task appears in the feed and the search filter narrows to it", async ({
    page,
  }) => {
    const taskTitle = uniqueTaskTitle("E2E feed task")
    const description =
      "Need a hand carrying a few moving boxes up to the third floor tomorrow afternoon."

    await loginAsSeededUser(page)

    // ── Post the task ───────────────────────────────────────────────────────
    await page.goto("/post-task")
    await expect(page.getByLabel("Task title")).toBeVisible()

    await page.getByLabel("Task title").fill(taskTitle)

    // The description field is a Tiptap rich-text editor (no native input).
    // Focus the contenteditable region and type — Tiptap handles real key
    // events, so this drives the underlying form state via its input handler.
    const editor = page.locator('[contenteditable="true"]').first()
    await editor.click()
    await editor.pressSequentially(description)

    // Pick the first seeded category — the reference data ships several, and
    // their exact names don't matter for the test's intent.
    await page.getByLabel("Category").click()
    await page.getByRole("option").first().click()

    // Default compensation type is "Points" — leave the radio group alone.
    await page.getByRole("button", { name: "Post task" }).click()

    // A successful create navigates to /tasks/{guid}; assert that before
    // moving on so we know the backend accepted the task.
    await page.waitForURL(/\/tasks\/[0-9a-f-]{36}$/i)

    // ── Verify the new task is visible in the feed ──────────────────────────
    await page.goto("/feed")
    await expect(
      page.getByRole("heading", { name: "Local tasks" })
    ).toBeVisible()

    const cardTitle = page.getByRole("heading", { name: taskTitle })
    await expect(cardTitle).toBeVisible()

    // ── Search filter finds it ──────────────────────────────────────────────
    const search = page.getByLabel("Search tasks, skills, locations")
    await search.fill(taskTitle)
    await expect(cardTitle).toBeVisible()

    // A query that can't possibly match should drop the task from the list.
    await search.fill(`${taskTitle}-impossible-suffix-zzz`)
    await expect(cardTitle).toHaveCount(0)
  })
})
