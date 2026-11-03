import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import {
  apiErrorDetail,
  apiErrorStatus,
  applyApiErrors,
  clearApiErrors,
  controlError,
  resolveFormApiError,
  STALE_RECORD_MESSAGE,
  extractApiFieldErrors,
  isStaleRecordError,
} from './http-error';

const httpError = (status: number, detail?: Record<string, string[]>) =>
  new HttpErrorResponse({ status, error: { title: 'Error de validación', detail } });

describe('extractApiFieldErrors', () => {
  it('une los mensajes de cada clave y normaliza la inicial', () => {
    const err = httpError(400, { ZonaHoraria: ['No existe.', 'Use America/New_York.'], ciudad: ['Requerida.'] });
    expect(extractApiFieldErrors(err)).toEqual({
      zonaHoraria: 'No existe. Use America/New_York.',
      ciudad: 'Requerida.',
    });
  });

  it('devuelve vacío cuando la respuesta no trae detail', () => {
    expect(extractApiFieldErrors(httpError(500))).toEqual({});
    expect(extractApiFieldErrors(new Error('red'))).toEqual({});
  });
});

describe('applyApiErrors', () => {
  const buildForm = () =>
    new FormGroup({
      ciudad: new FormControl('', Validators.required),
      paisId: new FormControl('DO'),
    });

  it('marca el control de cada clave conocida y devuelve el resto para el banner', () => {
    const form = buildForm();
    const err = httpError(400, { ciudad: ['Indique la ciudad.'], territorio: ['País distinto.'], otro: ['Sin campo.'] });

    const result = applyApiErrors(form, err, { ciudad: 'ciudad', territorio: 'paisId' });

    expect(result).toEqual({ fields: ['ciudad', 'paisId'], banner: 'Sin campo.' });
    expect(form.get('ciudad')!.getError('api')).toBe('Indique la ciudad.');
    expect(form.get('ciudad')!.hasError('required')).toBe(true);
    expect(form.get('paisId')!.getError('api')).toBe('País distinto.');
    expect(form.get('paisId')!.touched).toBe(true);
  });

  it('el error del servidor desaparece cuando el usuario corrige el campo', () => {
    const form = buildForm();
    applyApiErrors(form, httpError(400, { ciudad: ['Indique la ciudad.'] }), { ciudad: 'ciudad' });

    form.get('ciudad')!.setValue('Madrid');

    expect(form.get('ciudad')!.errors).toBeNull();
  });

  it('clearApiErrors quita el error del servidor y conserva la validación propia', () => {
    const form = buildForm();
    const fieldByKey = { ciudad: 'ciudad', territorio: 'paisId' };
    applyApiErrors(form, httpError(400, { ciudad: ['x'], territorio: ['y'] }), fieldByKey);

    clearApiErrors(form, fieldByKey);

    expect(form.get('paisId')!.errors).toBeNull();
    expect(form.get('ciudad')!.errors).toEqual({ required: true });
  });

  it('no marca nada cuando la respuesta no trae detail', () => {
    expect(applyApiErrors(buildForm(), httpError(500), { ciudad: 'ciudad' })).toEqual({
      fields: [],
      banner: '',
    });
  });
});

describe('estado del error', () => {
  it('lee el código HTTP y reconoce el registro desactualizado (412)', () => {
    expect(apiErrorStatus(httpError(403))).toBe(403);
    expect(apiErrorStatus(new Error('red'))).toBe(0);
    expect(isStaleRecordError(httpError(412))).toBe(true);
    expect(isStaleRecordError(httpError(409))).toBe(false);
  });
});

describe('resolveFormApiError', () => {
  const form = () => new FormGroup({ ciudad: new FormControl('') });
  const fieldByKey = { ciudad: 'ciudad' };

  it('403: devuelve el mensaje del servidor para el toast', () => {
    const result = resolveFormApiError(form(), httpError(403, { permiso: ['Sin permiso.'] }), fieldByKey, 'x');
    expect(result).toEqual({ forbidden: 'Sin permiso.', banner: '', stale: false });
  });

  it('412: banner de recarga', () => {
    const result = resolveFormApiError(form(), httpError(412), fieldByKey, 'x');
    expect(result).toEqual({ forbidden: '', banner: STALE_RECORD_MESSAGE, stale: true });
  });

  it('claves conocidas al campo, sin banner; desconocidas al banner', () => {
    const f = form();
    expect(resolveFormApiError(f, httpError(400, { ciudad: ['Falta.'] }), fieldByKey, 'x').banner).toBe('');
    expect(f.get('ciudad')!.getError('api')).toBe('Falta.');
    expect(
      resolveFormApiError(form(), httpError(409, { solapamiento: ['Se solapa.'] }), fieldByKey, 'x').banner,
    ).toBe('Se solapa.');
  });

  it('sin detalle: el banner muestra el título o el texto de respaldo', () => {
    expect(resolveFormApiError(form(), httpError(500), fieldByKey, 'x').banner).toBe('Error de validación');
    expect(resolveFormApiError(form(), {}, fieldByKey, 'No se pudo guardar').banner).toBe(
      'No se pudo guardar',
    );
  });
});

describe('controlError', () => {
  it('prioriza el mensaje del servidor y espera a que el campo se toque para el resto', () => {
    const control = new FormControl('', [Validators.required, Validators.maxLength(3)]);
    expect(controlError(control, 'Requerido')).toBe('');
    control.markAsTouched();
    expect(controlError(control, 'Requerido')).toBe('Requerido');
    control.setValue('abcd');
    expect(controlError(control)).toBe('Máximo 3 caracteres');
    control.setErrors({ api: 'Del servidor' });
    expect(controlError(control)).toBe('Del servidor');
  });
});

describe('apiErrorDetail', () => {
  it('devuelve los mensajes de la clave sin unir', () => {
    const err = httpError(409, { suscriptores: ['Embajada en Madrid', 'Consulado en Sevilla'] });
    expect(apiErrorDetail(err, 'suscriptores')).toEqual(['Embajada en Madrid', 'Consulado en Sevilla']);
    expect(apiErrorDetail(err, 'otra')).toEqual([]);
    expect(apiErrorDetail(new Error('red'), 'suscriptores')).toEqual([]);
  });
});
