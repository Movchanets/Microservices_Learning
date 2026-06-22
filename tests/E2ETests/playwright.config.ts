import { defineConfig, devices } from "@playwright/test";
import * as path from "path";

const AUTH_DIR=path.join(__dirname, "playwright/.auth");

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 1,
  workers: process.env.CI ? 3 : undefined,
  timeout: 60_000,
  expect: { timeout: 10_000 },

  reporter: process.env.CI
    ? [["html"], ["junit", { outputFile: "results/junit.xml" }], ["allure-playwright"]]
    : [["list"], ["html"], ["allure-playwright"]],

  use: {
    baseURL: process.env.BASE_URL || "http://localhost:4201",
    testIdAttribute: "data-testid",
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
    screenshot: "on-first-retry",
    video: "on-first-retry",
    trace: "on-first-retry",
  },

  metadata: {
    environment: process.env.CI ? "ci" : "local",
  },

  globalSetup: require.resolve("./globalSetup"),
  globalTeardown: require.resolve("./globalTeardown"),

  projects: [
    // ── Auth Setup ────────────────────────────────────────
    {
      name: "auth-setup",
      testMatch: /auth\.setup\.ts$/,
    },

    // ── Unauthenticated Tests ─────────────────────────────
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
      },
      dependencies: ["auth-setup"],
      testIgnore: [
        /\/orders\/.*\.spec\.ts$/,
        /\/checkout\/.*\.spec\.ts$/,
        /\/buyer\/.*\.spec\.ts$/,
        /\/profile-hub\.spec\.ts$/,
        /\/seller\/.*\.spec\.ts$/,
        /\/admin\/.*\.spec\.ts$/,
        /\/auth\/profile\.spec\.ts$/,
        /\/layout-auth\.spec\.ts$/,
      ],
    },

    // ── Buyer-Authenticated Tests ─────────────────────────
    {
      name: "buyer",
      use: {
        ...devices["Desktop Chrome"],
        storageState: path.join(AUTH_DIR, "buyer/state.json"),
      },
      dependencies: ["auth-setup"],
      testMatch: [
        /\/orders\/.*\.spec\.ts$/,
        /\/checkout\/.*\.spec\.ts$/,
        /\/buyer\/.*\.spec\.ts$/,
        /\/profile-hub\.spec\.ts$/,
        /\/layout-auth\.spec\.ts$/,
      ],
    },

    // ── Seller-Authenticated Tests ────────────────────────
    {
      name: "seller",
      use: {
        ...devices["Desktop Chrome"],
        storageState: path.join(AUTH_DIR, "seller/state.json"),
      },
      dependencies: ["auth-setup"],
      testMatch: [
        /\/seller\/.*\.spec\.ts$/,
      ],
    },

    // ── Admin-Authenticated Tests ─────────────────────────
    {
      name: "admin",
      use: {
        ...devices["Desktop Chrome"],
        storageState: path.join(AUTH_DIR, "admin/state.json"),
      },
      dependencies: ["auth-setup"],
      testMatch: [
        /\/admin\/.*\.spec\.ts$/,
        /\/auth\/profile\.spec\.ts$/,
      ],
    },
  ],
});
