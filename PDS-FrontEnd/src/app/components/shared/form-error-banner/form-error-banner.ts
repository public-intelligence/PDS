import { Component, input, output } from '@angular/core';

/** Banner de error al inicio de un formulario (§12.2); ofrece «Recargar» en un 412. */
@Component({
  selector: 'app-form-error-banner',
  templateUrl: './form-error-banner.html',
})
export class FormErrorBanner {
  readonly message = input('');
  readonly canReload = input(false);
  readonly reload = output<void>();
}
