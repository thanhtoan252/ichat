import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { HlmProgress, HlmProgressIndicator } from '@app/ui/progress';
import type { UploadProgress } from '../model/document.model';

/** Presentational. The bar shown while a file is being sent. */
@Component({
  selector: 'app-upload-progress',
  imports: [HlmProgress, HlmProgressIndicator],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'bg-card block rounded-xl border p-3.5' },
  templateUrl: './upload-progress.html',
})
export class UploadProgressBar {
  public readonly upload = input.required<UploadProgress>();
}
