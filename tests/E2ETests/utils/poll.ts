/**
 * Polling utility for async conditions.
 */

export interface PollOptions {
  maxAttempts?: number;
  delayMs?: number;
  label?: string;
}

/**
 * Polls an async condition with configurable backoff.
 * Returns the first truthy result, or throws after maxAttempts.
 */
export async function poll<T>(
  fn: () => Promise<T>,
  options: PollOptions = {}
): Promise<T> {
  const { maxAttempts = 20, delayMs = 1000, label = 'condition' } = options;

  for (let i = 0; i < maxAttempts; i++) {
    const result = await fn();
    if (result) return result;
    console.log(`Polling ${label}... attempt ${i + 1}/${maxAttempts}`);
    await new Promise((r) => setTimeout(r, delayMs));
  }
  throw new Error(`Polling ${label} timed out after ${maxAttempts} attempts`);
}
