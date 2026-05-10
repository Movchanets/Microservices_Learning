import { defineConfig, devices } from "@playwright/test";
import * as path from "path";

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "html",

  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: "http://localhost:4200",

    /* Enforce data-testid requirement */
    testIdAttribute: "data-testid",

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: "on-first-retry",
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],

  /* Run your local dev server before starting the tests */
  webServer: {
    // Start the .NET Aspire AppHost which spins up the ApiGateway, Identity API, and Angular frontend
    command:
      "dotnet run --project ../../src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj",
    url: "http://localhost:4200",
    reuseExistingServer: !process.env.CI,
    timeout: 300 * 1000,
    env: {
      // Set to testing environment so the backend knows to provision test data if needed
      ASPNETCORE_ENVIRONMENT: "Testing",
      TEST_USER_EMAIL: "buyer@test.com",
      TEST_USER_PASSWORD: "P@ssw0rd",
    },
  },
});
