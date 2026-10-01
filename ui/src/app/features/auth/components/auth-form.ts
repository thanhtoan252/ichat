import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { HlmButton } from '@app/ui/button';
import { HlmInput } from '@app/ui/input';
import { HlmLabel } from '@app/ui/label';
import type { Credentials } from '@app/core/auth/auth.model';

/** Presentational. Asks for the two things a sign-in needs. */
@Component({
  selector: 'app-auth-form',
  imports: [ReactiveFormsModule, NgIcon, HlmButton, HlmInput, HlmLabel],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './auth-form.html',
})
export class AuthForm {
  public readonly submitting = input(false);
  /** Set when the API rejected the last attempt, so the reason stays on screen. */
  public readonly failure = input<string | null>(null);

  public readonly submitted = output<Credentials>();

  protected readonly form = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected onSubmit(event: Event): void {
    event.preventDefault();

    if (this.form.invalid || this.submitting()) {
      return;
    }

    const { userName, password } = this.form.getRawValue();

    this.submitted.emit({ userName: userName.trim(), password });
  }
}
