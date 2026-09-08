import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';

/** Presentational. The centred card both auth screens sit in. */
@Component({
  selector: 'app-auth-shell',
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'grid min-h-dvh place-items-center px-4 py-10' },
  templateUrl: './auth-shell.html',
})
export class AuthShell {
  public readonly heading = input.required<string>();
  public readonly subheading = input.required<string>();
}
