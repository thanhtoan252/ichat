import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthApi } from './auth.api';
import type { AuthResponseDto, UserDto } from './auth.dto';
import { AuthStore } from './auth.store';

function session(overrides: Partial<AuthResponseDto> = {}): AuthResponseDto {
  return {
    accessToken: 'token-1',
    expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
    ...overrides,
  };
}

function profile(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: 'u-1',
    userName: 'alice',
    displayName: 'Alice',
    role: 'User',
    isActive: true,
    createdAt: new Date().toISOString(),
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

  it('restores a session from the refresh cookie, then reads the profile', async () => {
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      me: vi.fn().mockResolvedValue(profile()),
    });

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

  it('does not report a session when the profile call fails', async () => {
    // A token whose /me answers 401 is a token the API has stopped honouring. Reporting
    // 'authenticated' with a null user would let the guards through to a screen that
    // renders nobody.
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      me: vi.fn().mockRejectedValue(new Error('401')),
    });

    await store.restoreSession();

    expect(store.status()).toBe('anonymous');
    expect(store.user()).toBeNull();
  });

  it('reports the Admin role', async () => {
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      me: vi.fn().mockResolvedValue(profile({ role: 'Admin' })),
    });

    await store.restoreSession();

    expect(store.isAdmin()).toBe(true);
  });

  it('is only authenticated once the role is known, so adminGuard cannot read too early', async () => {
    let releaseProfile: (user: UserDto) => void = () => undefined;
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      me: vi.fn().mockReturnValue(
        new Promise<UserDto>((resolve) => {
          releaseProfile = resolve;
        }),
      ),
    });

    const restoring = store.restoreSession();

    // Refresh has already answered; the profile has not. Flipping status here would let
    // adminGuard ask isAdmin() while it is still false and bounce an administrator.
    expect(store.status()).toBe('unknown');

    releaseProfile(profile({ role: 'Admin' }));
    await restoring;

    expect(store.status()).toBe('authenticated');
    expect(store.isAdmin()).toBe(true);
  });

  it('reuses a token that is still comfortably valid', async () => {
    const refresh = vi.fn().mockResolvedValue(session());
    const store = makeStore({ refresh, me: vi.fn().mockResolvedValue(profile()) });

    await store.restoreSession();
    const token = await store.getAccessToken();

    expect(token).toBe('token-1');
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('refreshes a token that is about to expire without re-reading the profile', async () => {
    const refresh = vi
      .fn()
      .mockResolvedValueOnce(session({ expiresAt: new Date(Date.now() + 5_000).toISOString() }))
      .mockResolvedValueOnce(session({ accessToken: 'token-2' }));

    const me = vi.fn().mockResolvedValue(profile());
    const store = makeStore({ refresh, me });

    await store.restoreSession();

    // 5s of life is inside the skew window, so it must not be handed out as-is.
    expect(await store.getAccessToken()).toBe('token-2');
    expect(refresh).toHaveBeenCalledTimes(2);

    // The reader has not changed. Re-reading /me on every token rotation would put a
    // second round-trip on the critical path of an ordinary request.
    expect(me).toHaveBeenCalledTimes(1);
  });

  it('coalesces concurrent refreshes into one call', async () => {
    // Three parallel requests must not rotate the refresh token three times — the API
    // revokes the previous one on every rotation, so two of them would come back dead.
    const refresh = vi.fn().mockResolvedValue(session());
    const store = makeStore({ refresh, me: vi.fn().mockResolvedValue(profile()) });

    const [a, b, c] = await Promise.all([
      store.getAccessToken(),
      store.getAccessToken(),
      store.getAccessToken(),
    ]);

    expect(refresh).toHaveBeenCalledTimes(1);
    expect([a, b, c]).toEqual(['token-1', 'token-1', 'token-1']);
  });

  it('signs in and reads the profile of the account that just signed in', async () => {
    const store = makeStore({
      login: vi.fn().mockResolvedValue(session({ accessToken: 'token-login' })),
      me: vi.fn().mockResolvedValue(profile({ userName: 'bob', displayName: 'Bob' })),
    });

    await store.login({ userName: 'bob', password: 'secret' });

    expect(store.status()).toBe('authenticated');
    expect(store.user()?.userName).toBe('bob');
    expect(await store.getAccessToken()).toBe('token-login');
  });

  it('clears the session on logout without throwing, even when the API call fails', async () => {
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      me: vi.fn().mockResolvedValue(profile()),
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
    const store = makeStore({
      refresh: vi.fn().mockResolvedValue(session()),
      me: vi.fn().mockResolvedValue(profile()),
    });

    await store.restoreSession();
    store.forceLogout();

    expect(store.isAuthenticated()).toBe(false);
  });
});
