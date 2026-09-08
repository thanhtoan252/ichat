import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthStore } from './core/auth/auth.store';
import { routes } from './app.routes';

/**
 * These exercise the real route table, not a mock of it.
 *
 * They exist because the shell and the auth screens both sit at the empty path, and
 * whichever comes first wins any URL its children can absorb. Getting that order wrong
 * produced a `/` that rendered nothing and never reached a guard, which looked exactly
 * like "the app is not protected".
 */
function configure(auth: Partial<AuthStore>) {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideRouter(routes),
      { provide: AuthStore, useValue: auth },
    ],
  });

  return TestBed.inject(Router);
}

function store(overrides: Record<string, unknown> = {}): Partial<AuthStore> {
  return {
    status: () => 'anonymous',
    isAuthenticated: () => false,
    isAdmin: () => false,
    restoreSession: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  } as unknown as Partial<AuthStore>;
}

function signedIn(isAdmin = false): Partial<AuthStore> {
  return store({
    status: () => 'authenticated',
    isAuthenticated: () => true,
    isAdmin: () => isAdmin,
  });
}

describe('routes, anonymous', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it.each(['/chat', '/documents', '/settings', '/retrieval'])(
    'bounces %s to the login screen, carrying the destination',
    async (url) => {
      const router = configure(store());

      await router.navigateByUrl(url);

      expect(router.url).toBe(`/login?returnUrl=${encodeURIComponent(url)}`);
    },
  );

  it('bounces the root instead of rendering a blank page', async () => {
    const router = configure(store());

    await router.navigateByUrl('/');

    expect(router.url).toMatch(/^\/login/);
  });

  it('bounces an unknown URL, which redirects to the root', async () => {
    const router = configure(store());

    await router.navigateByUrl('/nothing-here');

    expect(router.url).toMatch(/^\/login/);
  });

  it.each(['/login'])('lets %s through', async (url) => {
    const router = configure(store());

    await router.navigateByUrl(url);

    expect(router.url).toBe(url);
  });

  it('waits for the bootstrap refresh before judging a hard reload', async () => {
    // The session starts as 'unknown'. Deciding before it settles would bounce a reader
    // who is in fact signed in, on every reload.
    let status = 'unknown';
    const router = configure(
      store({
        status: () => status,
        isAuthenticated: () => status === 'authenticated',
        restoreSession: vi.fn().mockImplementation(async () => {
          status = 'authenticated';
        }),
      }),
    );

    await router.navigateByUrl('/chat');

    expect(router.url).toBe('/chat');
  });
});

describe('routes, signed in', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it.each(['/chat', '/documents', '/settings'])('lets a plain user reach %s', async (url) => {
    const router = configure(signedIn());

    await router.navigateByUrl(url);

    expect(router.url).toBe(url);
  });

  it('keeps a plain user out of /retrieval', async () => {
    const url = '/retrieval';
    const router = configure(signedIn());

    await router.navigateByUrl(url);

    // Back to their own part of the app, not to a login screen that would read as
    // "your password stopped working".
    expect(router.url).toBe('/chat');
  });

  it('lets an administrator reach /retrieval', async () => {
    const url = '/retrieval';
    const router = configure(signedIn(true));

    await router.navigateByUrl(url);

    expect(router.url).toBe(url);
  });

  it('sends the root to chat', async () => {
    const router = configure(signedIn());

    await router.navigateByUrl('/');

    expect(router.url).toBe('/chat');
  });
});
