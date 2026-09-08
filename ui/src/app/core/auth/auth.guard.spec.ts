import { TestBed } from '@angular/core/testing';
import { Router, type ActivatedRouteSnapshot, type RouterStateSnapshot } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { adminGuard, authGuard } from './auth.guard';
import { AuthStore } from './auth.store';

type GuardResult = boolean | { toString(): string };

function run(
  guard: typeof authGuard | typeof adminGuard,
  store: Partial<AuthStore>,
  url = '/chat',
): Promise<GuardResult> {
  TestBed.configureTestingModule({
    providers: [
      { provide: AuthStore, useValue: store },
      {
        provide: Router,
        useValue: {
          createUrlTree: (commands: string[], extras?: { queryParams?: Record<string, string> }) =>
            ({
              toString: () =>
                `${commands.join('/')}${
                  extras?.queryParams ? `?returnUrl=${extras.queryParams['returnUrl']}` : ''
                }`,
            }) as never,
        },
      },
    ],
  });

  return TestBed.runInInjectionContext(
    () =>
      guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot) as Promise<GuardResult>,
  );
}

function store(overrides: Record<string, unknown>) {
  return {
    status: () => 'authenticated',
    isAuthenticated: () => true,
    isAdmin: () => false,
    restoreSession: vi.fn(),
    ...overrides,
  } as unknown as Partial<AuthStore>;
}

describe('authGuard', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('lets a signed-in reader through', async () => {
    expect(await run(authGuard, store({}))).toBe(true);
  });

  it('waits for the bootstrap refresh before deciding', async () => {
    const restoreSession = vi.fn().mockResolvedValue(undefined);
    let status = 'unknown';

    const result = await run(
      authGuard,
      store({
        status: () => status,
        isAuthenticated: () => status === 'authenticated',
        restoreSession: restoreSession.mockImplementation(async () => {
          status = 'authenticated';
        }),
      }),
    );

    // Without the wait, a hard reload of /chat would be judged mid-refresh and bounce
    // a reader who is in fact signed in.
    expect(restoreSession).toHaveBeenCalledOnce();
    expect(result).toBe(true);
  });

  it('sends an anonymous reader to the login screen carrying the destination', async () => {
    const result = await run(
      authGuard,
      store({ status: () => 'anonymous', isAuthenticated: () => false }),
      '/documents',
    );

    expect(String(result)).toBe('/login?returnUrl=/documents');
  });
});

describe('adminGuard', () => {
  beforeEach(() => TestBed.resetTestingModule());

  it('lets an administrator through', async () => {
    expect(await run(adminGuard, store({ isAdmin: () => true }))).toBe(true);
  });

  it('sends a signed-in non-administrator back to chat rather than to login', async () => {
    // Asking them to sign in again would read as "your password stopped working".
    expect(String(await run(adminGuard, store({}), '/retrieval'))).toBe('/chat');
  });

  it('sends an anonymous reader to the login screen', async () => {
    const result = await run(
      adminGuard,
      store({ status: () => 'anonymous', isAuthenticated: () => false }),
      '/retrieval',
    );

    expect(String(result)).toBe('/login?returnUrl=/retrieval');
  });
});
