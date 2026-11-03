import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { slugify } from '../../utils/slugify';

/**
 * Punto de entrada del menú "Organigrama" para el usuario autenticado. Resuelve la organización
 * (tenant) actual desde el token y redirige al organigrama público multitenant /public/:slug.
 * Si no hay acrónimo/nombre, cae a /public con organizationId (o la organización por defecto).
 */
@Component({
  selector: 'app-organigrama-redirect',
  template: '',
})
export class OrganigramaRedirectComponent {
  constructor() {
    const auth = inject(AuthService);
    const router = inject(Router);

    const token = auth.decodeToken();
    const slug = slugify(token?.organization_acronym || token?.organization_name);

    if (slug) {
      router.navigate(['/public', slug]);
    } else if (token?.organization_id) {
      router.navigate(['/public'], { queryParams: { organizationId: token.organization_id } });
    } else {
      router.navigate(['/public']);
    }
  }
}
