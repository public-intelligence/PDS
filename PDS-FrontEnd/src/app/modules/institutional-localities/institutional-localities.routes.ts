import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./localities-list/localities-list.component').then((m) => m.LocalitiesListComponent),
  },
  {
    path: 'mapa',
    loadComponent: () =>
      import('./localities-map/localities-map.component').then((m) => m.LocalitiesMapComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./locality-detail/locality-detail.component').then((m) => m.LocalityDetailComponent),
  },
];
