import { Directive } from '@angular/core';
import { classes } from '@app/ui/utils';

export const hlmSmall = 'text-sm font-medium leading-none';

@Directive({
  selector: '[hlmSmall]',
})
export class HlmSmall {
  constructor() {
    classes(() => hlmSmall);
  }
}
