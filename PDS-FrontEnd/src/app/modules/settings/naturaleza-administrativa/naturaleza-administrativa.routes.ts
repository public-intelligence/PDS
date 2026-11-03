import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./naturaleza-administrativa.component').then((m) => m.NaturalezaAdministrativaComponent),
  },
];
