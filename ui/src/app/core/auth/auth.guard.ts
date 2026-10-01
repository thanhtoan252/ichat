import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthStore } from './auth.store';

/**
 * Both guards wait for the bootstrap refresh before deciding. Without that wait a hard
 * reload of `/chat` would be judged while the session is still 'unknown' and bounce a
 * signed-in reader to the login screen.
 */
async function settle(auth: AuthStore): Promise<void> {
  if (auth.status() === 'unknown') {
    await auth.restoreSession();
  }
}

export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  await settle(auth);

  if (auth.isAuthenticated()) {
    return true;
  }

  // Carry the destination so signing in lands where the reader was actually going.
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const adminGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  await settle(auth);

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  // A signed-in reader without the role is not asked to sign in again — that would look
  // like their password stopped working. They go back to the part of the app that is theirs.
  return auth.isAdmin() ? true : router.createUrlTree(['/chat']);
};
