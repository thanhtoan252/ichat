import type { AuthResponseDto, UserDto } from './auth.dto';
import type { AuthUser, Session } from './auth.model';

export function toAuthUser(dto: UserDto): AuthUser {
  return {
    id: dto.id,
    userName: dto.userName,
    displayName: dto.displayName,
    role: dto.role,
    isActive: dto.isActive,
    createdAt: dto.createdAt,
  };
}

export function toSession(dto: AuthResponseDto): Session {
  return {
    accessToken: dto.accessToken,
    expiresAt: dto.expiresAt,
    user: toAuthUser(dto.user),
  };
}
