import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./occupacional-group.component').then((m) => m.OccupacionalGroupComponent),
  },
];
