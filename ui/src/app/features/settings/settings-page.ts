import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HlmBadge } from '@app/ui/badge';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSkeleton } from '@app/ui/skeleton';
import { ThemeService } from '@app/core/theme/theme.service';
import { ProviderCard } from './components/provider-card';
import { SettingsToolbar } from './components/settings-toolbar';
import { ThemePicker } from './components/theme-picker';
import { SettingsStore } from './settings.store';

/**
 * Container. Reports the active providers and owns the local appearance setting.
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
  protected readonly skeletonRows = [0, 1, 2];

  constructor() {
    void this.store.load();
  }
}
