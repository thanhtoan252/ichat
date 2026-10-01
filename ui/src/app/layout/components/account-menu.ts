import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import type { AuthUser } from '@app/core/auth/auth.model';

/** Presentational. Who is signed in, and the way out. */
@Component({
  selector: 'app-account-menu',
  imports: [NgIcon, HlmButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex min-w-0 flex-1 items-center gap-2' },
  templateUrl: './account-menu.html',
})
export class AccountMenu {
  public readonly user = input.required<AuthUser | null>();

  public readonly signOut = output<void>();
}
