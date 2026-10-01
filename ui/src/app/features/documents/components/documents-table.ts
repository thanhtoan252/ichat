import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmBadge, type BadgeVariants } from '@app/ui/badge';
import { HlmButton } from '@app/ui/button';
import { HlmTable, HlmTBody, HlmTd, HlmTh, HlmTHead, HlmTr } from '@app/ui/table';
import { HlmTooltip } from '@app/ui/tooltip';
import type { DocumentStatus, DocumentSummary } from '../data/document.model';

interface StatusStyle {
  readonly variant: NonNullable<BadgeVariants['variant']>;
  readonly icon: string;
}

const STATUS_STYLES: Readonly<Record<DocumentStatus, StatusStyle>> = {
  Indexed: { variant: 'secondary', icon: 'lucideCircleCheck' },
  Processing: { variant: 'outline', icon: 'lucideLoaderCircle' },
  Pending: { variant: 'outline', icon: 'lucideClock' },
  Failed: { variant: 'destructive', icon: 'lucideCircleAlert' },
};

/**
 * Presentational. One row per document.
 *
 * The failure reason gets its own row rather than a tooltip: a document that silently
 * never indexes is the most common way a RAG deployment goes quietly wrong, so the
 * reason has to be readable without hovering.
 */
@Component({
  selector: 'app-documents-table',
  imports: [
    DatePipe,
    DecimalPipe,
    NgIcon,
    HlmBadge,
    HlmButton,
    HlmTable,
    HlmTBody,
    HlmTd,
    HlmTh,
    HlmTHead,
    HlmTr,
    HlmTooltip,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './documents-table.html',
})
export class DocumentsTable {
  public readonly documents = input.required<readonly DocumentSummary[]>();
  /** Deleting is administrator-only; a reader without the role gets no dead button. */
  public readonly canManage = input(false);

  public readonly remove = output<DocumentSummary>();

  protected statusStyle(status: DocumentStatus): StatusStyle {
    return STATUS_STYLES[status];
  }

  protected formatSize(bytes: number): string {
    return formatFileSize(bytes);
  }
}

/** Exported for the unit test; the component only forwards to it. */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(0)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
