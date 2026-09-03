import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { HlmSeparator } from '@app/ui/separator';
import { HlmSidebarTrigger } from '@app/ui/sidebar';

/** Presentational. Header actions for the knowledge base. */
@Component({
  selector: 'app-documents-toolbar',
  imports: [NgIcon, HlmButton, HlmSeparator, HlmSidebarTrigger],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex h-14 shrink-0 items-center gap-2 border-b px-3 sm:px-4' },
  templateUrl: './documents-toolbar.html',
})
export class DocumentsToolbar {
  public readonly reindex = output<void>();
  public readonly filesPicked = output<readonly File[]>();

  protected onPicked(input: HTMLInputElement): void {
    this.filesPicked.emit(Array.from(input.files ?? []));
    // Clearing lets the same file be picked twice in a row.
    input.value = '';
  }
}
