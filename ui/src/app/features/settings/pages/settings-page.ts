import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HlmBadge } from '@app/ui/badge';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSkeleton } from '@app/ui/skeleton';
import { AuthStore } from '@app/core/auth/auth.store';
import { ThemeService } from '@app/core/theme/theme.service';
import { ProviderCard } from '../components/provider-card';
import { SettingsToolbar } from '../components/settings-toolbar';
import { ThemePicker } from '../components/theme-picker';
import { SettingsStore } from '../state/settings.store';

/**
 * Container. Reports the active providers to an administrator, and owns the local
 * appearance setting for everyone.
 *
 * Providers are server configuration — the API exposes no way to change them from a
 * client and never returns a key — so this page reports rather than edits.
 */
@Component({
  selector: 'app-settings-page',
  providers: [SettingsStore],
  imports: [HlmBadge, HlmSeparator, HlmSkeleton, ProviderCard, SettingsToolbar, ThemePicker],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  templateUrl: './settings-page.html',
})
export class SettingsPage {
  protected readonly store = inject(SettingsStore);
  protected readonly theme = inject(ThemeService);
  protected readonly auth = inject(AuthStore);
  protected readonly skeletonRows = [0, 1, 2];

  constructor() {
    // The provider catalog is an administrator endpoint. Everyone can still open this
    // page — appearance lives here too — but asking for the catalog would only 403.
    if (this.auth.isAdmin()) {
      void this.store.load();
    }
  }
}
