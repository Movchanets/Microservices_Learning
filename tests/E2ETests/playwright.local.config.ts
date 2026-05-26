import { defineConfig, devices } from "@playwright/test";

// Local override: skips globalSetup since AppHost is already running via Aspire
export default defineConfig({
  testDir: "./tests",
  fullyParallel: false,
  forbidOnly: false,
  retries: 1,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 10_000 },

  reporter: [["list"], ["html"]],

  use: {
    baseURL: process.env.BASE_URL || "http://localhost:4201",
    testIdAttribute: "data-testid",
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    screenshot: "on-first-retry",
    video: "on-first-retry",
    trace: "on-first-retry",
  },

  // No globalSetup / globalTeardown — Aspire is already running

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
