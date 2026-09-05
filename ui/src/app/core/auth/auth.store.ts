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

    if (!(await this.refresh())) {
      return;
    }

    try {
      await this.settleProfile();
    } catch {
      // settleProfile has already cleared the session. Bootstrap must never reject: the
      // guards await it without a catch of their own, so throwing here would break route
      // resolution outright instead of landing the reader on /login.
    }
  }

  public async login(credentials: Credentials): Promise<void> {
    // A new session means a new identity: the old profile has to go first, or
    // settleProfile() sees one already there and keeps the previous reader on screen.
    this.userSignal.set(null);
    this.applyTokens(toSession(await this.api.login(credentials)));

    await this.settleProfile();
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
      this.applyTokens(toSession(await this.api.refresh()));

      return this.accessToken;
    } catch {
      // No cookie, or one the API has revoked. Either way there is no session to keep.
      this.clear();

      return null;
    }
  }

  /**
   * Reads the profile, then — and only then — reports the session as authenticated.
   *
   * Both halves of that order are load-bearing. `adminGuard` calls `isAdmin()` the moment
   * status stops being 'unknown', so flipping it before the role is known sends an
   * administrator to /chat on every hard reload. And this runs OUTSIDE `runRefresh`:
   * `me()` goes through the interceptor, which asks for a token, and asking while the
   * shared refresh promise is still in flight would await the very call it is inside.
   */
  private async settleProfile(): Promise<void> {
    try {
      if (!this.userSignal()) {
        this.userSignal.set(toAuthUser(await this.api.me()));
      }
    } catch (error) {
      // A token the profile call will not answer for is not a session worth keeping.
      this.clear();

      throw error;
    }

    this.statusSignal.set('authenticated');
  }

  private applyTokens(session: Session): void {
    this.accessToken = session.accessToken;
    this.expiresAt = new Date(session.expiresAt).getTime();
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
