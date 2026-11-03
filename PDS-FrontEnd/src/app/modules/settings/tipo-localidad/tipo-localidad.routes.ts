import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./tipo-localidad.component').then((m) => m.TipoLocalidadComponent),
  },
];
