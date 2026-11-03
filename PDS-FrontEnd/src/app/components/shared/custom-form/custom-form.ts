import { Component, input, output, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-custom-form',
  templateUrl: './custom-form.html',
  imports: [ReactiveFormsModule],
})
export class CustomForm {
  readonly formGroup = input.required<FormGroup>();
  readonly title = input('');
  readonly description = input('');

  readonly formSubmit = output<Record<string, unknown>>();
  readonly formInvalid = output<void>();

  protected submitted = signal(false);

  protected handleSubmit(): void {
    this.submitted.set(true);

    if (this.formGroup().invalid) {
      this.formGroup().markAllAsTouched();
      this.formInvalid.emit();
      return;
    }

    this.formSubmit.emit(this.formGroup().value);
  }
}
