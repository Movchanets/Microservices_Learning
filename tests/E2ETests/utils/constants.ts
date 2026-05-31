/**
 * Shared timeouts for E2E tests.
 *
 * Use these instead of magic numbers to keep tests readable
 * and easy to tune in one place.
 */
export const TIMEOUTS = {
  /** Angular SSR hydration — wait for interactive elements */
  hydration: 10_000,

  /** Standard element visibility wait */
  element: 10_000,

  /** Slow API responses (cold start, search, pagination) */
  api: 15_000,

  /** Navigation / page load */
  navigation: 15_000,

  /** Quick assertions (button enabled, value present) */
  quick: 3_000,

  /** Retry-stable fill verification */
  fillRetry: 2_000,

  /** Slow external resources (scrapers, cold starts) */
  slow: 60_000,
} as const;
