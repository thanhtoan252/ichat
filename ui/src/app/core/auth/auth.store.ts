import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthApi } from './auth.api';
import { toAuthUser, toSession } from './auth.mapper';
import type { AuthUser, Credentials, Session } from './auth.model';

/** 'unknown' means the bootstrap refresh has not answered yet — guards must wait for it. */
export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous';

/** Refresh a little before the token actually dies, so a slow request cannot land expired. */
const EXPIRY_SKEW_MS = 30_000;

/**
 * The single owner of the session.
 *
 * The access token is held in a plain field rather than a signal or `localStorage`: it
 * must never be rendered, and never survive a tab close. What does survive is the
 * httpOnly refresh cookie, which this store trades for a new access token — on startup,
 * and again whenever the current one is about to expire.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);

  private accessToken: string | null = null;
  private expiresAt = 0;
  /** Concurrent callers share one refresh: three parallel requests must not rotate three times. */
  private inFlightRefresh: Promise<string | null> | null = null;

  private readonly userSignal = signal<AuthUser | null>(null);
  private readonly statusSignal = signal<AuthStatus>('unknown');

  public readonly user = this.userSignal.asReadonly();
  public readonly status = this.statusSignal.asReadonly();

  public readonly isAuthenticated = computed(() => this.statusSignal() === 'authenticated');
  public readonly isAdmin = computed(() => this.userSignal()?.role === 'Admin');

  /** Runs once before the first render, so a reload does not read as "signed out". */
  public async restoreSession(): Promise<void> {
    if (this.statusSignal() !== 'unknown') {
      return;
    }

    await this.refresh();
  }

  public async login(credentials: Credentials): Promise<void> {
    this.apply(toSession(await this.api.login(credentials)));
  }

  /**
   * Never throws. The reader asked to leave, so a server that could not be reached must
   * not become their problem — the refresh token expires on its own, while rethrowing
   * here used to abort the caller before it could navigate away, leaving them signed
   * out in state but still sitting inside the app.
   */
  public async logout(): Promise<void> {
    try {
      await this.api.logout();
    } catch {
      // Nothing to recover: the local session is cleared either way.
    } finally {
      this.clear();
    }
  }

  /** Drops the session without calling the API — for a refresh that came back 401. */
  public forceLogout(): void {
    this.clear();
  }

  /**
   * The token to put on the next request, refreshing first when it is about to expire.
   * Returns null when there is no session, and callers then send no header at all.
   */
  public async getAccessToken(): Promise<string | null> {
    if (this.accessToken && Date.now() < this.expiresAt - EXPIRY_SKEW_MS) {
      return this.accessToken;
    }

    return this.refresh();
  }

  private refresh(): Promise<string | null> {
    this.inFlightRefresh ??= this.runRefresh().finally(() => {
      this.inFlightRefresh = null;
    });

    return this.inFlightRefresh;
  }

  private async runRefresh(): Promise<string | null> {
    try {
      this.apply(toSession(await this.api.refresh()));

      return this.accessToken;
    } catch {
      // No cookie, or one the API has revoked. Either way there is no session to keep.
      this.clear();

      return null;
    }
  }

  private apply(session: Session): void {
    this.accessToken = session.accessToken;
    this.expiresAt = new Date(session.expiresAt).getTime();
    this.userSignal.set(session.user);
    this.statusSignal.set('authenticated');
  }

  private clear(): void {
    this.accessToken = null;
    this.expiresAt = 0;
    this.userSignal.set(null);
    this.statusSignal.set('anonymous');
  }

  /** Re-reads the profile, so a role changed by an administrator shows up without a reload. */
  public async reloadProfile(): Promise<void> {
    if (!this.isAuthenticated()) {
      return;
    }

    this.userSignal.set(toAuthUser(await this.api.me()));
  }
}
