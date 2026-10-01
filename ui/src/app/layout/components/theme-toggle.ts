import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import {
  HlmDropdownMenu,
  HlmDropdownMenuRadio,
  HlmDropdownMenuRadioIndicator,
  HlmDropdownMenuTrigger,
} from '@app/ui/dropdown-menu';
import { THEME_OPTIONS, type ThemePreference } from '@app/core/theme/theme.service';

/** Presentational. Shows the current theme and asks for a different one. */
@Component({
  selector: 'app-theme-toggle',
  imports: [
    NgIcon,
    HlmButton,
    HlmDropdownMenu,
    HlmDropdownMenuTrigger,
    HlmDropdownMenuRadio,
    HlmDropdownMenuRadioIndicator,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './theme-toggle.html',
})
export class ThemeToggle {
  public readonly preference = input.required<ThemePreference>();
  public readonly resolved = input.required<'light' | 'dark'>();

  public readonly preferenceChange = output<ThemePreference>();

  protected readonly options = THEME_OPTIONS;
}
