import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_BASE_URL, API_V1 } from '@app/core/api/api.config';
import type { AuthResponseDto, LoginRequestDto, UserDto } from './auth.dto';

/**
 * Every call here sends credentials: the refresh token lives in an httpOnly cookie
 * scoped to `/api/v1/auth`, and without `withCredentials` the browser would leave it out.
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private get root(): string {
    return `${this.baseUrl}${API_V1}/auth`;
  }

  login(request: LoginRequestDto): Promise<AuthResponseDto> {
    return firstValueFrom(
      this.http.post<AuthResponseDto>(`${this.root}/login`, request, { withCredentials: true }),
    );
  }

  refresh(): Promise<AuthResponseDto> {
    return firstValueFrom(
      this.http.post<AuthResponseDto>(`${this.root}/refresh`, null, { withCredentials: true }),
    );
  }

  logout(): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${this.root}/logout`, null, { withCredentials: true }),
    );
  }

  me(): Promise<UserDto> {
    return firstValueFrom(this.http.get<UserDto>(`${this.root}/me`, { withCredentials: true }));
  }
}
