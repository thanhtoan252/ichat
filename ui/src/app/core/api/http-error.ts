import { HttpErrorResponse } from '@angular/common/http';
import type { ProblemDetails } from '@app/shared/util/api-envelope.model';

/**
 * Turns any transport failure into one sentence fit for a toast.
 *
 * The API answers with RFC 7807, and validation failures put the useful part in
 * `errors` rather than `detail`, so both shapes are unwrapped here instead of at
 * every call site.
 */
export function describeHttpError(error: unknown, fallback = 'Something went wrong.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return error instanceof Error ? error.message : fallback;
  }

  // 0 is a browser-level network failure; 502/504 is a proxy that could not reach the
  // API at all. Both mean the same thing to a reader, and "Bad Gateway" does not say it.
  if (error.status === 0 || error.status === 502 || error.status === 504) {
    return 'Cannot reach the API. Is the backend running?';
  }

  if (error.status === 503) {
    return 'The API is up but not ready yet. Check /health/ready.';
  }

  const problem = error.error as ProblemDetails | string | null;

  if (typeof problem === 'string' && problem.trim().length > 0) {
    return problem;
  }

  if (problem && typeof problem === 'object') {
    const firstValidationMessage = Object.values(problem.errors ?? {})
      .flat()
      .find((message) => message.length > 0);

    const message = firstValidationMessage ?? problem.detail ?? problem.title;

    if (message) {
      return message;
    }
  }

  // Last resort for the two auth statuses: the API does answer them with a detail, so
  // these only show when it could not — and "Unauthorized" tells a reader nothing.
  if (error.status === 401) {
    return 'Your session has expired. Sign in again.';
  }

  if (error.status === 403) {
    return 'You do not have permission to do that.';
  }

  return error.statusText || fallback;
}
