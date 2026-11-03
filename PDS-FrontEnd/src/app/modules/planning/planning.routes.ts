import { Routes } from '@angular/router';
import { PlanningShellComponent } from './planning-shell.component';

/**
 * Rutas del MOCKUP de Planificación (demo para validación con stakeholders).
 * Todas cuelgan del shell que superpone el selector de persona de la demo.
 */
export const routes: Routes = [
  {
    path: '',
    component: PlanningShellComponent,
    children: [
      {
        path: 'inicio',
        redirectTo: 'plan-anual',
        pathMatch: 'full',
      },
      {
        path: 'mi-trabajo',
        redirectTo: 'plan-anual',
        pathMatch: 'full',
      },
      {
        path: 'ciclos',
        loadComponent: () =>
          import('./pages/ciclos/ciclos-list.component').then((m) => m.CiclosListComponent),
      },
      {
        path: 'ciclos/nuevo',
        loadComponent: () =>
          import('./pages/ciclos/ciclo-nuevo.component').then((m) => m.CicloNuevoComponent),
      },
      {
        path: 'pei',
        loadComponent: () => import('./pages/pei/pei.component').then((m) => m.PeiComponent),
      },
      {
        path: 'plan-anual',
        loadComponent: () =>
          import('./pages/plan-anual/planes-list.component').then((m) => m.PlanesListComponent),
      },
      {
        path: 'plan-anual/:planId',
        loadComponent: () =>
          import('./pages/plan-anual/plan-workspace.component').then((m) => m.PlanWorkspaceComponent),
      },
      {
        path: 'plan-anual/:planId/revisar',
        loadComponent: () =>
          import('./pages/plan-anual/plan-revisar.component').then((m) => m.PlanRevisarComponent),
      },
      {
        path: 'plan-anual/:planId/ajustes',
        loadComponent: () =>
          import('./pages/ajustes/ajustes-plan.component').then((m) => m.AjustesPlanComponent),
      },
      {
        path: 'requerimientos',
        loadComponent: () =>
          import('./pages/requerimientos/requerimientos-bandeja.component').then(
            (m) => m.RequerimientosBandejaComponent,
          ),
      },
      {
        path: 'requerimientos/:reqId',
        loadComponent: () =>
          import('./pages/requerimientos/requerimiento-detalle.component').then(
            (m) => m.RequerimientoDetalleComponent,
          ),
      },
      {
        path: 'consolidacion',
        loadComponent: () =>
          import('./pages/consolidacion/consolidacion.component').then((m) => m.ConsolidacionComponent),
      },
      {
        path: 'seguimiento',
        loadComponent: () =>
          import('./pages/seguimiento/seguimiento-list.component').then((m) => m.SeguimientoListComponent),
      },
      {
        path: 'seguimiento/:registroId',
        loadComponent: () =>
          import('./pages/seguimiento/seguimiento-captura.component').then(
            (m) => m.SeguimientoCapturaComponent,
          ),
      },
      {
        path: 'aprobacion',
        loadComponent: () =>
          import('./pages/aprobacion/aprobacion.component').then((m) => m.AprobacionComponent),
      },
      {
        path: 'reportes-pds',
        loadComponent: () =>
          import('./pages/reportes/reportes-pds.component').then((m) => m.ReportesPdsComponent),
      },
      {
        path: 'configuracion-pds',
        children: [
          {
            path: 'unidades',
            loadComponent: () =>
              import('./pages/configuracion/unidades-config.component').then(
                (m) => m.UnidadesConfigComponent,
              ),
          },
          {
            path: 'plantillas',
            loadComponent: () =>
              import('./pages/configuracion/plantillas.component').then((m) => m.ConfigPlantillasComponent),
          },
          {
            path: 'maestra-articulos',
            loadComponent: () =>
              import('./pages/configuracion/maestra-articulos.component').then(
                (m) => m.ConfigMaestraArticulosComponent,
              ),
          },
          {
            path: 'catalogos-regulatorios',
            loadComponent: () =>
              import('./pages/configuracion/catalogos-regulatorios.component').then(
                (m) => m.ConfigCatalogosRegulatoriosComponent,
              ),
          },
          {
            path: 'reglas-ruteo',
            loadComponent: () =>
              import('./pages/configuracion/reglas-ruteo-consolidador.component').then(
                (m) => m.ConfigReglasRuteoConsolidadorComponent,
              ),
          },
          {
            path: 'validacion-mapeo',
            loadComponent: () =>
              import('./pages/configuracion/validacion-mapeo.component').then(
                (m) => m.ConfigValidacionMapeoComponent,
              ),
          },
          {
            path: 'techos',
            loadComponent: () =>
              import('./pages/configuracion/techos.component').then((m) => m.ConfigTechosComponent),
          },
          // La antigua pantalla de rubros la reemplaza la Maestra de Artículos.
          { path: 'rubros', redirectTo: 'maestra-articulos', pathMatch: 'full' },
          {
            path: 'reglas-ruteo-consolidador',
            redirectTo: 'reglas-ruteo',
            pathMatch: 'full',
          },
        ],
      },
    ],
  },
];
