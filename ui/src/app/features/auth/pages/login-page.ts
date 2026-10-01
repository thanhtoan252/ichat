import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { describeHttpError } from '@app/core/api/http-error';
import { AuthStore } from '@app/core/auth/auth.store';
import type { Credentials } from '@app/core/auth/auth.model';
import { AuthForm } from '../components/auth-form';
import { AuthShell } from '../components/auth-shell';

/** Container. Signs in and returns the reader to wherever the guard interrupted them. */
@Component({
  selector: 'app-login-page',
  imports: [AuthForm, AuthShell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  /** Bound from the query string by `withComponentInputBinding`. */
  public readonly returnUrl = input<string | undefined>(undefined);

  protected readonly submitting = signal(false);
  protected readonly failure = signal<string | null>(null);

  protected async signIn(credentials: Credentials): Promise<void> {
    this.submitting.set(true);
    this.failure.set(null);

    try {
      await this.auth.login(credentials);
      await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
    } catch (error) {
      // Stays inline rather than in a toast: it belongs to the field the reader is fixing.
      this.failure.set(describeHttpError(error, 'Could not sign in.'));
    } finally {
      this.submitting.set(false);
    }
  }
}

/**
 * `returnUrl` arrives from the query string, so it is whatever a link says it is.
 * Anything but a plain in-app path — an absolute URL, or the `//host` form that browsers
 * read as protocol-relative — falls back to /chat, so a crafted link cannot turn the
 * login screen into a redirector onto someone else's site.
 *
 * Exported for the unit test; the page only forwards to it.
 */
export function safeReturnUrl(returnUrl: string | undefined): string {
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/chat';
}
