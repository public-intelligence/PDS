import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl } from '@angular/forms';

/**
 * Extrae un mensaje legible y específico desde una respuesta de error del API.
 *
 * El backend responde con la forma `{ title, detail: { <campo>: string[] } }`. Se priorizan los
 * mensajes de `detail` (los específicos, p. ej. de validación) sobre `title` (genérico), y por
 * último el `fallback`.
 */
export function extractApiErrors(
  err: unknown,
  fallback = 'Ocurrió un error inesperado.',
): string {
  const body = err instanceof HttpErrorResponse ? err.error : (err as { error?: unknown })?.error ?? err;

  // Algunos errores llegan como texto plano.
  if (typeof body === 'string' && body.trim()) return body;

  const detail = (body as { detail?: unknown })?.detail;
  if (detail && typeof detail === 'object') {
    const messages = Object.values(detail as Record<string, unknown>)
      .flatMap((v) => (Array.isArray(v) ? v : [v]))
      .filter((m): m is string => typeof m === 'string' && m.trim().length > 0);
    if (messages.length) return messages.join(' ');
  }

  const title = (body as { title?: unknown })?.title;
  if (typeof title === 'string' && title.trim()) return title;

  const message = (body as { message?: unknown })?.message;
  if (typeof message === 'string' && message.trim()) return message;

  return fallback;
}

/** Banner del 412 (§12.2): el registro cambió desde que se cargó. */
export const STALE_RECORD_MESSAGE =
  'Otra persona modificó este registro. Recargue para ver los cambios.';

/** Código HTTP del error; 0 si no es una respuesta del API. */
export function apiErrorStatus(err: unknown): number {
  return err instanceof HttpErrorResponse ? err.status : 0;
}

export function isStaleRecordError(err: unknown): boolean {
  return apiErrorStatus(err) === 412;
}

/** Mensajes de `detail` por clave (§10.5), con la inicial en minúscula como los campos del API. */
export function extractApiFieldErrors(err: unknown): Record<string, string> {
  const detail = (err instanceof HttpErrorResponse ? err.error : null)?.detail;
  if (!detail || typeof detail !== 'object') return {};

  const errors: Record<string, string> = {};
  for (const [key, value] of Object.entries(detail as Record<string, unknown>)) {
    const message = (Array.isArray(value) ? value : [value])
      .filter((m): m is string => typeof m === 'string' && m.trim().length > 0)
      .join(' ');
    if (message) errors[key.charAt(0).toLowerCase() + key.slice(1)] = message;
  }
  return errors;
}

export interface ApiFormErrors {
  /** Controles que recibieron un mensaje del servidor. */
  fields: string[];
  /** Mensajes cuya clave no corresponde a ningún campo: van al banner del formulario. */
  banner: string;
}

/**
 * Lleva cada clave de `detail` a su control como error `api` (§12.2). `fieldByKey` enumera las
 * claves que el formulario sabe mostrar; el error se limpia solo cuando el usuario edita el campo.
 */
export function applyApiErrors(
  form: AbstractControl,
  err: unknown,
  fieldByKey: Record<string, string>,
): ApiFormErrors {
  const fields: string[] = [];
  const unmapped: string[] = [];

  for (const [key, message] of Object.entries(extractApiFieldErrors(err))) {
    const field = fieldByKey[key];
    const control = field ? form.get(field) : null;
    if (!field || !control) {
      unmapped.push(message);
      continue;
    }
    control.setErrors({ ...control.errors, api: message });
    control.markAsTouched();
    fields.push(field);
  }

  return { fields, banner: unmapped.join(' ') };
}

/** Quita los errores `api` antes de reintentar: el servidor vuelve a decidir si siguen vigentes. */
export function clearApiErrors(form: AbstractControl, fieldByKey: Record<string, string>): void {
  for (const field of new Set(Object.values(fieldByKey))) {
    form.get(field)?.updateValueAndValidity({ emitEvent: false });
  }
}

/** Qué mostrar tras un guardado rechazado (§12.2). */
export interface FormApiError {
  /** 403: mensaje para el toast; el servidor es la autoridad y la pantalla debe refrescarse. */
  forbidden: string;
  /** Texto del banner del formulario; vacío si todo quedó junto a su campo. */
  banner: string;
  /** 412: otro usuario modificó el registro; el banner ofrece «Recargar». */
  stale: boolean;
}

/**
 * Reparte un error de guardado: 403 al toast, 412 al banner de recarga y el resto a los campos de
 * `fieldByKey`; lo que no tenga campo (o no traiga detalle) va al banner.
 */
export function resolveFormApiError(
  form: AbstractControl,
  err: unknown,
  fieldByKey: Record<string, string>,
  fallback: string,
): FormApiError {
  if (apiErrorStatus(err) === 403) {
    return { forbidden: extractApiErrors(err, fallback), banner: '', stale: false };
  }
  if (isStaleRecordError(err)) {
    return { forbidden: '', banner: STALE_RECORD_MESSAGE, stale: true };
  }
  const { fields, banner } = applyApiErrors(form, err, fieldByKey);
  return {
    forbidden: '',
    banner: banner || (fields.length ? '' : extractApiErrors(err, fallback)),
    stale: false,
  };
}

/** Mensaje de un control: el del servidor o, si ya se tocó, el de su validación propia. */
export function controlError(control: AbstractControl, requiredMessage = 'Campo requerido'): string {
  if (control.hasError('api')) return control.getError('api');
  if (!control.touched) return '';
  if (control.hasError('required')) return requiredMessage;
  if (control.hasError('maxlength')) {
    return `Máximo ${control.getError('maxlength').requiredLength} caracteres`;
  }
  return '';
}

/** Mensajes de una clave de `detail` sin unir (p. ej. la lista de suscriptores de un 409). */
export function apiErrorDetail(err: unknown, key: string): string[] {
  const value = (err instanceof HttpErrorResponse ? err.error : null)?.detail?.[key];
  return (Array.isArray(value) ? value : [value]).filter(
    (m): m is string => typeof m === 'string' && m.trim().length > 0,
  );
}
