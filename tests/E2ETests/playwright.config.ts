import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 1,
  workers: process.env.CI ? 3 : undefined,
  timeout: 60_000,
  expect: { timeout: 10_000 },

  reporter: process.env.CI
    ? [["html"], ["junit", { outputFile: "results/junit.xml" }]]
    : [["list"], ["html"]],

  use: {
    baseURL: process.env.BASE_URL || "http://localhost:4201",
    testIdAttribute: "data-testid",
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    screenshot: "on-first-retry",
    video: "on-first-retry",
    trace: "on-first-retry",
  },

  globalSetup: require.resolve("./globalSetup"),
  globalTeardown: require.resolve("./globalTeardown"),

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
