import { HttpErrorResponse } from '@angular/common/http';
import { HttpParams } from '@angular/common/http';

/**
 * Extracts a human-readable error message from an unknown error thrown
 * by Angular's HttpClient. Handles HttpErrorResponse, plain Error objects,
 * and fallback strings.
 *
 * Replaces unsafe `err as { error?: { error?: string } }` casts.
 */
export function extractHttpError(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    // Try common error shapes returned by ASP.NET APIs
    const body = err.error;
    if (typeof body === 'string') return body;
    if (body?.error) return body.error;
    if (body?.title) return body.title;
    if (body?.message) return body.message;
    return err.message || fallback;
  }
  if (err instanceof Error) return err.message;
  if (typeof err === 'string') return err;
  return fallback;
}

/**
 * Builds HttpParams from a record, filtering out nullish and empty-string values.
 * Eliminates repetitive `if (params.x) httpParams = httpParams.set(...)` blocks.
 *
 * @example
 * buildParams({ page: 1, categoryId: null, search: '' })
 * // → HttpParams with only { page: '1' }
 */
export function buildParams(
  record: Record<string, string | number | boolean | null | undefined>,
): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(record)) {
    if (value != null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
