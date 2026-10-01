/**
 * Wire types for `/auth`. Two things are deliberately absent from `AuthResponseDto`:
 * the refresh token, which only ever travels in an httpOnly cookie so no code on this
 * side can read it, and the user, which now comes from `GET /auth/me`.
 */

export type UserRoleDto = 'User' | 'Admin';

export interface UserDto {
  readonly id: string;
  readonly userName: string;
  readonly displayName: string;
  readonly role: UserRoleDto;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface AuthResponseDto {
  readonly accessToken: string;
  readonly expiresAt: string;
}

export interface LoginRequestDto {
  readonly userName: string;
  readonly password: string;
}
