import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSidebarTrigger } from '@app/ui/sidebar';

/** Presentational. Settings header. */
@Component({
  selector: 'app-settings-toolbar',
  imports: [NgIcon, HlmButton, HlmSeparator, HlmSidebarTrigger],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex h-14 shrink-0 items-center gap-2 border-b px-3 sm:px-4' },
  templateUrl: './settings-toolbar.html',
})
export class SettingsToolbar {
  /** Only the provider catalog can be refreshed, and only an administrator may read it. */
  public readonly canRefresh = input(false);

  public readonly refresh = output<void>();
}
