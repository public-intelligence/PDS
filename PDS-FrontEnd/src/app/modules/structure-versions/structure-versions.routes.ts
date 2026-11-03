import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./versions-list/versions-list.component').then((m) => m.VersionsListComponent),
  },
  {
    path: 'compare',
    loadComponent: () =>
      import('./version-compare/version-compare.component').then((m) => m.VersionCompareComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./version-detail/version-detail.component').then((m) => m.VersionDetailComponent),
  },
];
