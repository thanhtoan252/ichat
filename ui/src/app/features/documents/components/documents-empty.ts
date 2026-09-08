import { ChangeDetectionStrategy, Component } from '@angular/core';
import { NgIcon } from '@ng-icons/core';

/** Presentational. Shown when nothing has been ingested yet. */
@Component({
  selector: 'app-documents-empty',
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col items-center px-6 py-16 text-center' },
  templateUrl: './documents-empty.html',
})
export class DocumentsEmpty {}
