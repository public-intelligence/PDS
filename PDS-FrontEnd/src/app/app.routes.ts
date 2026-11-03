import { Routes } from '@angular/router';
import { MainLayout } from './components/ui/layout/main/main.layout/main.layout';
import { Dashboard } from './modules/dashboard/dashboard';
import { OauthGobId } from './modules/auth/oauth-gob-id/oauth-gob-id';
import { adminGuard } from './guards/admin.guard';
import { featureGuard } from './guards/feature.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: OauthGobId,
  },
  {
    // Organigrama público multitenant: /public/:slug identifica la organización. Ruta raíz SIN
    // guard ni layout autenticado.
    path: 'public/:slug',
    loadComponent: () =>
      import('./modules/public-org-chart/public-org-chart.component').then(
        (m) => m.PublicOrgChartComponent,
      ),
  },
  {
    // Sin slug: usa la organización por defecto del backend (o ?organizationId=).
    path: 'public',
    loadComponent: () =>
      import('./modules/public-org-chart/public-org-chart.component').then(
        (m) => m.PublicOrgChartComponent,
      ),
  },
  {
    // Acceso desde el menú lateral: resuelve la organización del usuario y redirige a /public/:slug.
    path: 'organigrama',
    loadComponent: () =>
      import('./modules/public-org-chart/organigrama-redirect.component').then(
        (m) => m.OrganigramaRedirectComponent,
      ),
  },
  {
    component: MainLayout,
    path: '',
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: Dashboard },
      {
        // Diagnóstico de acceso: sin permisos propios, porque quien NO tiene accesos es
        // justamente quien necesita entender qué le falta y a quién pedírselo.
        path: 'mi-acceso',
        loadComponent: () =>
          import('./modules/access/my-access.component').then((m) => m.MyAccessComponent),
      },
      {
        path: 'usuarios-roles',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./modules/access/user-access.component').then((m) => m.UserAccessComponent),
      },
      {
        path: 'structures',
        loadChildren: () => import('./modules/structures/structures.routes').then((m) => m.routes),
      },
      {
        path: 'structure-versions',
        loadChildren: () =>
          import('./modules/structure-versions/structure-versions.routes').then((m) => m.routes),
      },
      {
        path: 'positions',
        loadChildren: () => import('./modules/position/position.routes').then((m) => m.routes),
      },
      {
        // RF-RE: módulo de reportería e indicadores (dashboard + reportes exportables).
        path: 'reportes',
        loadChildren: () => import('./modules/reporting/reporting.routes').then((m) => m.routes),
      },
      {
        path: 'localidades',
        loadChildren: () =>
          import('./modules/institutional-localities/institutional-localities.routes').then(
            (m) => m.routes,
          ),
      },
      {
        // MOCKUP de Planificación (demo para stakeholders): inicio, mi trabajo,
        // ciclos, PEI, plan anual, requerimientos, consolidación, seguimiento,
        // aprobación y configuración PDS. Sin backend (estado en memoria).
        path: '',
        loadChildren: () => import('./modules/planning/planning.routes').then((m) => m.routes),
      },
      {
        path: 'settings',
        children: [
          {
            path: 'area-types',
            loadChildren: () =>
              import('./modules/settings/area-type/area-type.routes').then((m) => m.routes),
          },
          {
            path: 'servicios',
            loadChildren: () =>
              import('./modules/settings/servicio/servicio.routes').then((m) => m.routes),
          },
          {
            path: 'responsables',
            loadChildren: () =>
              import('./modules/settings/responsable/responsable.routes').then((m) => m.routes),
          },
          {
            path: 'tipos-localidad',
            loadChildren: () =>
              import('./modules/settings/tipo-localidad/tipo-localidad.routes').then(
                (m) => m.routes,
              ),
          },
          {
            path: 'naturalezas-administrativas',
            loadChildren: () =>
              import('./modules/settings/naturaleza-administrativa/naturaleza-administrativa.routes').then(
                (m) => m.routes,
              ),
          },
          {
            path: 'estados-localidad',
            loadChildren: () =>
              import('./modules/settings/estado-localidad/estado-localidad.routes').then(
                (m) => m.routes,
              ),
          },
          {
            path: 'calendarios-feriados',
            canMatch: [featureGuard('calendarioLaboral')],
            loadChildren: () =>
              import('./modules/settings/holiday-calendars/holiday-calendars.routes').then(
                (m) => m.routes,
              ),
          },
          {
            path: 'funciones-localidad',
            canMatch: [featureGuard('funcionesLocalidad')],
            loadChildren: () =>
              import('./modules/settings/funcion-localidad/funcion-localidad.routes').then(
                (m) => m.routes,
              ),
          },
          {
            path: 'occupational-groups',
            loadChildren: () =>
              import('./modules/settings/occupacional-group/occupacional-group.routes').then(
                (m) => m.routes,
              ),
          },
        ],
      },
    ],
  },
];
