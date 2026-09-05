import { HttpErrorResponse, type HttpInterceptorFn, type HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { API_V1 } from '@app/core/api/api.config';
import { AuthStore } from './auth.store';

/**
 * The only endpoints that must go out WITHOUT a bearer token. Two are anonymous by
 * design, and `refresh` is where the token itself comes from — routing it through here
 * would recurse.
 *
 * Deliberately not "everything under /auth": `logout` and `me` are authenticated calls
 * like any other, and skipping them made the API answer 401 to every sign-out.
 */
const ANONYMOUS_AUTH_PATHS = ['/auth/login', '/auth/refresh'] as const;

function isAnonymousAuthEndpoint(request: HttpRequest<unknown>): boolean {
  return ANONYMOUS_AUTH_PATHS.some((path) => request.url.includes(`${API_V1}${path}`));
}

function withBearer<T>(request: HttpRequest<T>, token: string): HttpRequest<T> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

/**
 * Attaches the access token, and gives a 401 exactly one second chance.
 *
 * The store already refreshes an expiring token before it is used, so a 401 here means
 * the server rejected a token this client still believed in — revoked, or the account
 * disabled. One forced refresh separates "our clock was wrong" from "the session is
 * over"; anything past that is a failure the caller has to see.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (isAnonymousAuthEndpoint(request)) {
    return next(request);
  }

  const auth = inject(AuthStore);
  const router = inject(Router);

  return from(auth.getAccessToken()).pipe(
    switchMap((token) => next(token ? withBearer(request, token) : request)),
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return from(auth.getAccessToken()).pipe(
        switchMap((token) => {
          if (!token) {
            auth.forceLogout();
            // Leaving the reader on a page they can no longer use hides the real state:
            // every later action fails one at a time until they guess what happened.
            void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });

            return throwError(() => error);
          }

          return next(withBearer(request, token));
        }),
      );
    }),
  );
};
