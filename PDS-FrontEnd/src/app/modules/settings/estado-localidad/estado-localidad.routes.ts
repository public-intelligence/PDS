import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./estado-localidad.component').then((m) => m.EstadoLocalidadComponent),
  },
];
