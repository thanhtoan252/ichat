import { InjectionToken } from '@angular/core';
import { environment } from '@env/environment';

/** Base URL of the IChat API. Empty means same origin (dev server proxy, or a co-hosted build). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiBaseUrl,
});

export const API_V1 = '/api/v1';
