import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./structures.component').then((m) => m.StructureComponent),
  },
];
