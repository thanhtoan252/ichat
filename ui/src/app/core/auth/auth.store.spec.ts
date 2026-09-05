import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from './auth.api';
import type { AuthResponseDto } from './auth.dto';
import { AuthStore } from './auth.store';

function session(overrides: Partial<AuthResponseDto> = {}): AuthResponseDto {
  return {
    accessToken: 'token-1',
    expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    user: {
      id: 'u-1',
      userName: 'alice',
      displayName: 'Alice',
      role: 'User',
      isActive: true,
      createdAt: new Date().toISOString(),
    },
    ...overrides,
  };
}

function makeStore(api: Partial<AuthApi>): AuthStore {
  TestBed.configureTestingModule({
    providers: [AuthStore, { provide: AuthApi, useValue: api }],
  });

  return TestBed.inject(AuthStore);
}

describe('AuthStore', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('starts in the unknown state so guards wait instead of redirecting', () => {
    const store = makeStore({ refresh: vi.fn() });

    expect(store.status()).toBe('unknown');
    expect(store.isAuthenticated()).toBe(false);
  });

  it('restores a session from the refresh cookie', async () => {
    const store = makeStore({ refresh: vi.fn().mockResolvedValue(session()) });

    await store.restoreSession();

    expect(store.status()).toBe('authenticated');
    expect(store.user()?.userName).toBe('alice');
    expect(store.isAdmin()).toBe(false);
  });

  it('settles as anonymous when there is no usable cookie', async () => {
    const store = makeStore({ refresh: vi.fn().mockRejectedValue(new Error('401')) });

    await store.restoreSession();

    // Must not stay 'unknown': the guard would then wait forever on every route.
    expect(store.status()).toBe('anonymous');
  });

  it('reports the Admin role', async () => {
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session({ user: { ...session().user, role: 'Admin' } })),
    });

    await store.restoreSession();

    expect(store.isAdmin()).toBe(true);
  });

  it('reuses a token that is still comfortably valid', async () => {
    const refresh = vi.fn().mockResolvedValue(session());
    const store = makeStore({ refresh });

    await store.restoreSession();
    const token = await store.getAccessToken();

    expect(token).toBe('token-1');
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('refreshes a token that is about to expire', async () => {
    const refresh = vi
      .fn()
      .mockResolvedValueOnce(session({ expiresAt: new Date(Date.now() + 5_000).toISOString() }))
      .mockResolvedValueOnce(session({ accessToken: 'token-2' }));

    const store = makeStore({ refresh });

    await store.restoreSession();

    // 5s of life is inside the skew window, so it must not be handed out as-is.
    expect(await store.getAccessToken()).toBe('token-2');
    expect(refresh).toHaveBeenCalledTimes(2);
  });

  it('coalesces concurrent refreshes into one call', async () => {
    // Three parallel requests must not rotate the refresh token three times — the API
    // revokes the previous one on every rotation, so two of them would come back dead.
    const refresh = vi.fn().mockResolvedValue(session());
    const store = makeStore({ refresh });

    const [a, b, c] = await Promise.all([
      store.getAccessToken(),
      store.getAccessToken(),
      store.getAccessToken(),
    ]);

    expect(refresh).toHaveBeenCalledTimes(1);
    expect([a, b, c]).toEqual(['token-1', 'token-1', 'token-1']);
  });

  it('clears the session on logout without throwing, even when the API call fails', async () => {
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      logout: vi.fn().mockRejectedValue(new Error('offline')),
    });

    await store.restoreSession();

    // Must resolve: the caller navigates to the login screen right after this, and a
    // rejection here aborts it — signed out in state, still sitting inside the app.
    await expect(store.logout()).resolves.toBeUndefined();

    expect(store.status()).toBe('anonymous');
    expect(store.user()).toBeNull();
  });

  it('drops the session when a forced logout is requested', async () => {
    const store = makeStore({ refresh: vi.fn().mockResolvedValue(session()) });

    await store.restoreSession();
    store.forceLogout();

    expect(store.isAuthenticated()).toBe(false);
  });
});
