/**
 * Wire types for `/auth`. The refresh token is deliberately absent: it only ever
 * travels in an httpOnly cookie, so no code on this side can read it.
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
  readonly user: UserDto;
}

export interface LoginRequestDto {
  readonly userName: string;
  readonly password: string;
}
