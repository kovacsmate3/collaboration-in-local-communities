# 2gather — Project Results

**Course:** Advanced Software Technology &nbsp;·&nbsp; **Coach:** Dániel Leskó
**Project:** Collaboration in Local Communities
**Team:** Sándor Baranyi (CT9XFJ), Babayev Ilkin (B96B92), Máté Kovács (U5BKY4), Adorján Nagy-Mohos (D5VD5E), Mátyás Székely (AMRIGW)
**Budapest, 2026**

> **2gather** is a bilingual (HU/EN) neighbour-help platform. Every member is both a **Seeker** (post a task) and a **Helper** (accept one). Discover people nearby, agree the terms in chat, complete the task, and build a public reputation — turning a neighbourhood into a self-sustaining micro-cooperation community.

---

## 1. Marketing material for end users

> **Deliverable:** short feature presentation/flier + a ≤2-minute product video.
> **Video:** ‹link to the ≤2-minute demo video — fill in before submission›
> **Flier / slide deck:** ‹link or `docs/results/2gather-flier.pdf` — fill in before submission›

### Why you need 2gather (end-user pitch)

You need a shelf mounted, a sofa moved, an hour of maths tutoring — and you don't have anyone nearby to ask. 2gather connects you with real people in your own area who can help, today.

| Feature | What it gives you |
|---|---|
| **Post a task in seconds** | Title, description, category, location, and whether it's paid, barter, or voluntary — visible to helpers right away. |
| **Find help nearby** | Browse the Helper Feed and filter by category, distance, and compensation. Location-aware so results are genuinely local. |
| **Skill matching** | Helpers see the tasks that match their listed skills first. |
| **Chat before you commit** | One-on-one in-app chat opens as soon as interest is expressed — agree details safely before meeting. |
| **Reputation you can trust** | Every completed task ends with a star rating + written review. Profiles show average rating, completed-task count, and recent reviews. |
| **Earn points for helping** | Helpers automatically earn platform points on every completed task. |
| **Your language** | Full Hungarian and English UI. |

---

## 2. Link to the testable product + how to try it

- **Live app:** ‹production URL — fill in / confirm before submission (brand domain: 2gather.hu)›
- **Admin KPI dashboard:** `‹live URL›/admin` (admin role required — read-only metrics)

### How to try it
1. Open the live app and click **Register** (email + password, or social login).
2. Accept the **Terms & Conditions** (mandatory one-time gate).
3. Complete your **profile**: name, bio, skills, workplace, position, location, photo.
4. On the **Seeker Feed**, click **Post a task** — fill in title, description, category, location, and compensation type.
5. Switch to the **Helper Feed**, use the filters (category / distance / compensation) to find a task, and **express interest / accept**.
6. A **chat** opens between seeker and helper — coordinate the details there.
7. Move the task through **Open → In Progress → Completed**; both parties confirm.
8. Leave a **rating + review**. Watch the **reputation** update on the profile.

### Run it locally (reviewers)
```bash
# Full stack: backend + frontend + Postgres + Cosmos emulator
docker compose up
```
See the repository [`README.md`](../../README.md) for prerequisites and IDE-based workflows.

---

## 3. Development-progress links (VCS · CI · bug tracker · agile)

| Resource | Link |
|---|---|
| **Version control (GitHub)** | https://github.com/kovacsmate3/collaboration-in-local-communities |
| **CI/CD (GitHub Actions)** | `.github/workflows/` — backend CI, frontend CI, E2E, Qodana code quality, and CD pipelines (GHCR image build → Azure Container Apps backend + Vercel frontend) |
| **Bug tracker (GitHub Issues)** | https://github.com/kovacsmate3/collaboration-in-local-communities/issues |
| **Agile board (GitHub Projects)** | Issues organised into Epics (milestones: Sprint 0–4) and a Kanban board |
| **Code quality** | Qodana (PR mode) + Codecov coverage integration |

**Process at a glance:** 1 setup sprint + 3 two-week development sprints + 1 polish/demo sprint (20 Apr – 13 Jun 2026). Every task tracked as an issue under a sprint milestone; PR-based workflow with required CI checks, StyleCop/ESLint gates, and a Sprint-1 retrospective (Assignment 4) feeding process improvements.

---

## 4. Documentation

### 4.1 User stories — implemented vs. not implemented (vs. planned MVP scope)

The MVP scope (Assignment 2) selected **10 user stories** (9 Must-Have + 1 Should-Have) across **7 modules**, plus 3 implicitly-covered stories, 3 deferred (Could-Have), and 2 excluded (Won't-Have).

#### MVP stories — built

| ID | User story | MoSCoW | Status | Evidence (issues) |
|----|------------|--------|--------|-------------------|
| US-01 | Find / post help (Seeker Feed, post a task) | Must | ✅ Implemented | #30, #37, #38, #236, #239 |
| US-04 | Search & filter by predefined categories | Must | ✅ Implemented | #13, #33, #41, #111 |
| US-09 | See tasks matching my skills | Must | ✅ Implemented | #19, #32, #35 |
| US-11 | Build reputation via completed tasks & feedback | Must | ✅ Implemented | #51, #52, #62, #63 |
| US-12 | Know if help is paid / point-based / voluntary | Must | ✅ Implemented | #38, #41 |
| US-14 | See ratings & written reviews | Must | ✅ Implemented | #51, #59, #62, #115 |
| US-15 | Chat securely before meeting | Must | ✅ Implemented | #46–#50, #56–#58, #117 |
| US-16 | Read a short helper description | Must | ✅ Implemented | #17, #24, #112 |
| US-02 | Location-based proximity discovery | Should | ✅ Implemented | #12, #34, #39, #42 |
| **US-10** | **Earn points/benefits for helping** | Must | ⚠️ **Partial** | earning + ledger entity done (#54, reward service); **balance/history endpoint (#53), points page (#64), and reward algorithm (#237) still open** |

**Supporting modules delivered** (platform prerequisites, not standalone stories): Registration & Onboarding, Login/Logout/Session, T&C gate, editable Profile + privacy toggle, Job/Task lifecycle management, bilingual HU/EN, and the Admin KPI dashboard. Issues: #14–#27, #36, #54, #55, #60, #61, #72, #73, #114, #116, #120, #132, #138.

#### Implicitly covered (no separate implementation required — served by Must-Haves)

| ID | Use case | Covered by |
|----|----------|-----------|
| US-06 | Newcomer discovery | Seeker/Helper Feeds (US-01, US-09) |
| US-07 | Furniture moving without social debt | Task posting + "Moving" category + compensation label (US-01, US-04, US-12) |
| US-08 | Tutoring requests | "Tutoring" category in the posting flow (US-04) |

#### Deferred — Could-Have (intentionally out of MVP, planned post-launch)

| ID | Feature | Fallback shipped in MVP |
|----|---------|-------------------------|
| US-03 | Typo-tolerant / fuzzy search | Category browsing + filters |
| US-17 | Smart digital agreements | Chat + task description + compensation label |
| US-18 | Advanced granular privacy controls | Basic public/private field toggle shipped (#114) |

#### Excluded — Won't-Have (infrastructure prerequisites beyond project scope)

| ID | Feature | Why excluded / mitigation |
|----|---------|---------------------------|
| US-05 | AI intelligent matching | Needs trained ML model on real transaction data; manual search + skill-tag surfacing covers ~80% of value |
| US-13 | Government ID verification | Needs third-party identity provider + legal compliance; trust addressed via profile photos, reviews (US-14), chat (US-15) |

#### MVP realization ratio

**10 / 10 MVP stories addressed; 9 fully implemented, 1 (US-10) partially** (points are earned and ledgered, but the user-facing balance/history view and the configurable reward algorithm are still open). All 7 planned modules are usable end-to-end: a user can register, post or find a task, chat, complete it, and leave a review.

> **Action before submission:** decide whether to (a) finish US-10's points page (#53/#64/#237) to reach a clean 10/10, or (b) submit as-is and report US-10 as "earning implemented, viewing/redeeming deferred." Either is defensible; (a) maximises the MVP Realization Ratio rubric score.

### 4.2 User statistics & KPI calculation vs. original target

**Chosen KPI — Activity KPI** (Assignment 2). It combines task throughput, engagement, and quality into one number, averaged per community:

```
Activity KPI = ( (Tasks / All Users) × Active Users × Completion Rate ) / Number of Communities
```

- **All Users** — registered accounts not labelled abandoned (abandoned after 3 months inactivity)
- **Active Users** — unique users with a task interaction (post/accept/complete/review) in the rolling window
- **Completion Rate** — tasks confirmed by both parties ÷ total posted (excludes tasks < 48 h old)
- **Communities** — locations with ≥ 30 registered users

**Original target (Assignment 2):**

| Metric | Week 1–2 | Week 3–4 | Month 2 | **Month 3 (project-end target)** | Month 6 (post-launch) |
|---|---|---|---|---|---|
| Registered users | 30 | 60 | 120 | **200** | 680 |
| Active users | 20 | 35 | 65 | **90** | 265 |
| Tasks posted | 10 | 25 | 45 | **70** | 215 |
| Completion rate | 1.00 | 0.85 | 0.80 | **0.75** | 0.70 |
| Communities | 1 | 1 | 1 | **1** | 2 |
| **Activity KPI** | 6.67 | 12.40 | 19.50 | **23.63** | 29.33 |

**Primary project commitment:** Activity KPI **≥ 23.63** by end of Month 3 (one established BME/ELTE community).

**Measurement baseline.** This Results submission is timed at the end of the **2-week soft launch**, so the actuals below are measured against the **Week 1–2 milestone** (Activity KPI target **6.67**) — *not* the Month-3 figure. Month-3 (23.63) remains the longer-term post-launch ambition shown in the table above.

**Actual measured results** — read live from the Admin KPI dashboard at `/admin` (it reports `RegisteredUsers`, `ActiveUsers7d`, `TasksPosted7d`, `CompletedTasks7d`, `CompletionRate7d`). Measured **2026-06-13**:

| Metric | Target (Week 1–2) | Actual (2026-06-13) | Achievement |
|---|---|---|---|
| Registered users | 30 | 33 | 110 % |
| Active users (7-day) | 20 | 33 | 165 % |
| Tasks posted (7-day) | 10 | 33 | 330 % |
| Completed tasks (7-day) | — | 33 | — |
| Completion rate | 1.00 | 1.00 | 100 % |
| Communities | 1 | 1 | 100 % |
| **Activity KPI** | **6.67** | **33.0** | **≈ 495 %** |

All 33 posted tasks are completed (both parties confirmed) → completion rate **33 / 33 = 1.00**, and all 33 registered users are active.

**Activity KPI calculation** (Week 1–2 window): `(( Tasks / All Users ) × Active Users × Completion Rate ) / Communities = (( 33 / 33 ) × 33 × 1.00 ) / 1 = 33.0`.

**KPI realization ratio** = Actual Activity KPI ÷ 6.67 = **33.0 ÷ 6.67 ≈ 4.95×** — the Week 1–2 target is met and substantially exceeded, reflecting an intensive soft launch in which essentially every registered user both posted and completed a task.

> **Note on measurement honesty:** the actuals above were read directly from the running platform's Admin KPI dashboard (`/admin`) on 2026-06-13 — not estimated. The data source is the 2-week soft launch, so the **Week 1–2 milestone** (Activity KPI 6.67) is the correct comparison baseline; the Month-3 figure (23.63) applies to the full post-launch horizon. All 33 registered users are active and all 33 posted tasks are completed, for a platform-wide completion rate of 1.00.

---

## Appendix — outstanding work at time of writing

Snapshot of open issues (verified against the codebase, 2026-06-10):

| Issue | Area | Bearing on Results submission |
|---|---|---|
| #70 / #71 / #69 / #246 | Production deploy, soft launch, Key Vault secrets, Data-Protection keys | **Gates deliverable #2** (the live product link). CD pipelines exist; confirm prod is actually live + soft-launch accounts created. |
| #53 / #64 / #237 | Points balance/ledger endpoint, points page, reward algorithm | **US-10 partial** — affects MVP Realization Ratio. |
| #67 | Security audit (OWASP Top 10) | In progress; close before launch. |
| #44 / #45 / #77 | E2E post-task test, demo seed data, full E2E run | #45 demo seed missing (helps the demo); #44 post-task E2E missing. |
| #74 / #75 / #76 / #184 / #244 | Empty/error states, UX polish + mobile QA, performance, FE quality, typography | Sprint-4 polish; improves demo/marketing quality. |
| #78 | Demo preparation (slides, script, backup video) | **Produces deliverable #1** (marketing material + video). |
