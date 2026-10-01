import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import {
  HlmAlertDialog,
  HlmAlertDialogAction,
  HlmAlertDialogCancel,
  HlmAlertDialogContent,
  HlmAlertDialogDescription,
  HlmAlertDialogFooter,
  HlmAlertDialogHeader,
  HlmAlertDialogPortal,
  HlmAlertDialogTitle,
} from '@app/ui/alert-dialog';

/**
 * Presentational. Asks before something that cannot be taken back.
 *
 * Driven by an `open` input rather than a trigger button, because the actions that need
 * confirming here come from a table row and a toolbar, not from the dialog's own markup.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [
    HlmAlertDialog,
    HlmAlertDialogAction,
    HlmAlertDialogCancel,
    HlmAlertDialogContent,
    HlmAlertDialogDescription,
    HlmAlertDialogFooter,
    HlmAlertDialogHeader,
    HlmAlertDialogPortal,
    HlmAlertDialogTitle,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <hlm-alert-dialog [state]="open() ? 'open' : 'closed'" (closed)="cancelled.emit()">
      <ng-template hlmAlertDialogPortal>
        <hlm-alert-dialog-content>
          <div hlmAlertDialogHeader>
            <h3 hlmAlertDialogTitle>{{ title() }}</h3>
            <p hlmAlertDialogDescription>{{ description() }}</p>
          </div>
          <div hlmAlertDialogFooter>
            <button hlmAlertDialogCancel (click)="cancelled.emit()">Cancel</button>
            <button hlmAlertDialogAction [variant]="variant()" (click)="confirmed.emit()">
              {{ confirmLabel() }}
            </button>
          </div>
        </hlm-alert-dialog-content>
      </ng-template>
    </hlm-alert-dialog>
  `,
})
export class ConfirmDialog {
  public readonly open = input.required<boolean>();
  public readonly title = input.required<string>();
  public readonly description = input.required<string>();
  public readonly confirmLabel = input('Confirm');
  public readonly variant = input<'default' | 'destructive'>('default');

  public readonly confirmed = output<void>();
  public readonly cancelled = output<void>();
}
