import { defineConfig, devices } from "@playwright/test";
import * as path from "path";

const AUTH_DIR = path.join(__dirname, "playwright/.auth");

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
        /profile-hub\.spec\.ts$/,
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
