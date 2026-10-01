import { Directive } from '@angular/core';
import { BrnSeparator } from '@spartan-ng/brain/separator';
import { classes } from '@app/ui/utils';

/**
 * Vertical separators take their height from the consumer (`h-5`, `self-stretch`, ...).
 * A default `self-stretch` here would win on source order over a plain `h-*` class and
 * pin the rule to the top of a centred flex row instead of stretching it.
 */
export const hlmSeparatorClass =
  'inline-flex shrink-0 bg-border data-horizontal:h-px data-horizontal:w-full data-vertical:w-px';

@Directive({
  selector: '[hlmSeparator],hlm-separator',
  hostDirectives: [{ directive: BrnSeparator, inputs: ['orientation', 'decorative'] }],
  host: {
    'data-slot': 'separator',
  },
})
export class HlmSeparator {
  constructor() {
    classes(() => hlmSeparatorClass);
  }
}
