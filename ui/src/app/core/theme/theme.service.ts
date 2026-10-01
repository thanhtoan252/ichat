import { DOCUMENT, Injectable, effect, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';

export interface ThemeOption {
  readonly value: ThemePreference;
  readonly label: string;
  readonly icon: string;
}

/** Shared by the sidebar toggle and the settings page, so the two never drift. */
export const THEME_OPTIONS: readonly ThemeOption[] = [
  { value: 'light', label: 'Light', icon: 'lucideSun' },
  { value: 'dark', label: 'Dark', icon: 'lucideMoon' },
  { value: 'system', label: 'System', icon: 'lucideMonitor' },
];

const STORAGE_KEY = 'ichat.theme';

/**
 * Owns the `.dark` class on `<html>`, which is what the spartan preset's dark variant
 * keys off. "system" is the default and keeps following the OS after the first paint.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly systemPrefersDark = signal(false);

  public readonly preference = signal<ThemePreference>(readStoredPreference());

  public readonly resolved = signal<'light' | 'dark'>('light');

  constructor() {
    const media = this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)');

    if (media) {
      this.systemPrefersDark.set(media.matches);
      media.addEventListener('change', (event) => this.systemPrefersDark.set(event.matches));
    }

    effect(() => {
      const preference = this.preference();
      const dark = preference === 'dark' || (preference === 'system' && this.systemPrefersDark());

      this.resolved.set(dark ? 'dark' : 'light');
      this.document.documentElement.classList.toggle('dark', dark);
      this.document.documentElement.style.colorScheme = dark ? 'dark' : 'light';

      try {
        this.document.defaultView?.localStorage.setItem(STORAGE_KEY, preference);
      } catch {
        // Private browsing can reject writes; the theme still applies for this session.
      }
    });
  }

  public set(preference: ThemePreference): void {
    this.preference.set(preference);
  }

  public toggle(): void {
    this.preference.set(this.resolved() === 'dark' ? 'light' : 'dark');
  }
}

function readStoredPreference(): ThemePreference {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);

    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored;
    }
  } catch {
    // Ignored: falls back to following the system.
  }

  return 'system';
}
