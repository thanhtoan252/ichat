import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { describeHttpError } from './http-error';

function response(init: { status: number; statusText?: string; error?: unknown }) {
  return new HttpErrorResponse({
    status: init.status,
    statusText: init.statusText ?? '',
    error: init.error ?? null,
  });
}

describe('describeHttpError', () => {
  it('reads a network failure as unreachable', () => {
    expect(describeHttpError(response({ status: 0 }))).toContain('Cannot reach the API');
  });

  it('reads a dead upstream behind the dev proxy as unreachable', () => {
    // Without this the reader sees "Bad Gateway", which says nothing about what to do.
    expect(describeHttpError(response({ status: 502, statusText: 'Bad Gateway' }))).toContain(
      'Cannot reach the API',
    );
    expect(describeHttpError(response({ status: 504, statusText: 'Gateway Timeout' }))).toContain(
      'Cannot reach the API',
    );
  });

  it('separates "not ready" from "not reachable"', () => {
    expect(describeHttpError(response({ status: 503 }))).toContain('not ready yet');
  });

  it('prefers the ProblemDetails detail', () => {
    const message = describeHttpError(
      response({ status: 400, error: { title: 'Bad Request', detail: 'The file is empty.' } }),
    );

    expect(message).toBe('The file is empty.');
  });

  it('unwraps the first validation message', () => {
    const message = describeHttpError(
      response({ status: 400, error: { errors: { file: ['The file exceeds the 20MB limit.'] } } }),
    );

    expect(message).toBe('The file exceeds the 20MB limit.');
  });

  it('falls back to the status text', () => {
    expect(describeHttpError(response({ status: 418, statusText: "I'm a teapot" }))).toBe(
      "I'm a teapot",
    );
  });

  it('handles a plain Error', () => {
    expect(describeHttpError(new Error('boom'))).toBe('boom');
  });
});
