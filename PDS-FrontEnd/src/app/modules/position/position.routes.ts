import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./position.component').then((m) => m.PositionComponent),
  },
  {
    path: 'details/:id',
    loadComponent: () => import('./postion-details/postion-details').then((m) => m.PostionDetails),
  },
];
