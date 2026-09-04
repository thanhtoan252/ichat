import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { THEME_OPTIONS, type ThemePreference } from '@app/core/theme/theme.service';

/** Presentational. The appearance choice, as a row of buttons. */
@Component({
  selector: 'app-theme-picker',
  imports: [NgIcon, HlmButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-wrap gap-2' },
  templateUrl: './theme-picker.html',
})
export class ThemePicker {
  public readonly preference = input.required<ThemePreference>();

  public readonly preferenceChange = output<ThemePreference>();

  protected readonly options = THEME_OPTIONS;
}
