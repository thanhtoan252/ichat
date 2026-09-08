import {
  HttpErrorResponse,
  HttpRequest,
  HttpResponse,
  type HttpHandlerFn,
} from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { authInterceptor } from './auth.interceptor';
import { AuthStore } from './auth.store';

const router = { url: '/chat', navigate: vi.fn() };

function run(store: Partial<AuthStore>, request: HttpRequest<unknown>, next: HttpHandlerFn) {
  TestBed.configureTestingModule({
    providers: [
      { provide: AuthStore, useValue: store },
      { provide: Router, useValue: router },
    ],
  });

  return TestBed.runInInjectionContext(() => firstValueFrom(authInterceptor(request, next)));
}

function get(url = '/api/v1/conversations'): HttpRequest<unknown> {
  return new HttpRequest('GET', url);
}

const OK = new HttpResponse({ status: 200 });

describe('authInterceptor', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
    router.navigate.mockClear();
  });

  it('attaches the access token', async () => {
    const next = vi.fn().mockReturnValue(of(OK));

    await run({ getAccessToken: vi.fn().mockResolvedValue('token-1') }, get(), next);

    expect(next.mock.calls[0][0].headers.get('Authorization')).toBe('Bearer token-1');
  });

  it('sends no header when there is no session', async () => {
    const next = vi.fn().mockReturnValue(of(OK));

    await run({ getAccessToken: vi.fn().mockResolvedValue(null) }, get(), next);

    expect(next.mock.calls[0][0].headers.has('Authorization')).toBe(false);
  });

  it.each(['/api/v1/auth/login', '/api/v1/auth/refresh'])(
    'leaves %s alone — anonymous, and refreshing must not recurse',
    async (url) => {
      const getAccessToken = vi.fn();
      const next = vi.fn().mockReturnValue(of(OK));

      await run({ getAccessToken }, get(url), next);

      expect(getAccessToken).not.toHaveBeenCalled();
      expect(next.mock.calls[0][0].headers.has('Authorization')).toBe(false);
    },
  );

  it.each(['/api/v1/auth/logout', '/api/v1/auth/me'])(
    'still signs %s — the API requires a token there',
    async (url) => {
      // Skipping these answered 401 to every sign-out, which then aborted the
      // navigation to the login screen.
      const next = vi.fn().mockReturnValue(of(OK));

      await run({ getAccessToken: vi.fn().mockResolvedValue('token-1') }, get(url), next);

      expect(next.mock.calls[0][0].headers.get('Authorization')).toBe('Bearer token-1');
    },
  );

  it('retries once with a fresh token after a 401', async () => {
    const getAccessToken = vi.fn().mockResolvedValueOnce('stale').mockResolvedValueOnce('fresh');

    const next = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 401 })))
      .mockReturnValueOnce(of(OK));

    const response = await run({ getAccessToken, forceLogout: vi.fn() }, get(), next);

    expect(next).toHaveBeenCalledTimes(2);
    expect(next.mock.calls[1][0].headers.get('Authorization')).toBe('Bearer fresh');
    expect(response).toBe(OK);
  });

  it('gives up and clears the session when the retry has no token', async () => {
    const forceLogout = vi.fn();
    const next = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));

    await expect(
      run(
        {
          getAccessToken: vi.fn().mockResolvedValueOnce('stale').mockResolvedValueOnce(null),
          forceLogout,
        },
        get(),
        next,
      ),
    ).rejects.toBeInstanceOf(HttpErrorResponse);

    expect(forceLogout).toHaveBeenCalledOnce();
    expect(next).toHaveBeenCalledTimes(1);
    // The reader is taken to the login screen instead of being left on a dead page.
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/chat' },
    });
  });

  it('does not retry a 403 — the role is wrong, not the token', async () => {
    const next = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 403 })));

    await expect(
      run({ getAccessToken: vi.fn().mockResolvedValue('token-1') }, get(), next),
    ).rejects.toBeInstanceOf(HttpErrorResponse);

    expect(next).toHaveBeenCalledTimes(1);
  });
});
