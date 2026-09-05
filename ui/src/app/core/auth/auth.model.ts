export type UserRole = 'User' | 'Admin';

export interface AuthUser {
  readonly id: string;
  readonly userName: string;
  readonly displayName: string;
  readonly role: UserRole;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface Session {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly user: AuthUser;
}

export interface Credentials {
  readonly userName: string;
  readonly password: string;
}
