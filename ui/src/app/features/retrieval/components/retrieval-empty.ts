import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgIcon } from '@ng-icons/core';

/** Presentational. Shown before the first trace has been run. */
@Component({
  selector: 'app-retrieval-empty',
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col items-center px-6 py-16 text-center' },
  templateUrl: './retrieval-empty.html',
})
export class RetrievalEmpty {}
