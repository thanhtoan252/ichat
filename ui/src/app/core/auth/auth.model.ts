export type UserRole = 'User' | 'Admin';

export interface AuthUser {
  readonly id: string;
  readonly userName: string;
  readonly displayName: string;
  readonly role: UserRole;
  readonly isActive: boolean;
  readonly createdAt: string;
}

/** The secret half of a session. Who it belongs to is read separately, from `/auth/me`. */
export interface Session {
  readonly accessToken: string;
  readonly expiresAt: string;
}

export interface Credentials {
  readonly userName: string;
  readonly password: string;
}
