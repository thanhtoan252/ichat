import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmBadge } from '@app/ui/badge';
import type { ProviderRow } from '../data/provider.model';

/** Presentational. One configured provider. */
@Component({
  selector: 'app-provider-card',
  imports: [NgIcon, HlmBadge],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'bg-card block rounded-xl border p-4' },
  templateUrl: './provider-card.html',
})
export class ProviderCard {
  public readonly row = input.required<ProviderRow>();
}
