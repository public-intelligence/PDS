import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./servicio.component').then((m) => m.ServicioComponent),
  },
];
