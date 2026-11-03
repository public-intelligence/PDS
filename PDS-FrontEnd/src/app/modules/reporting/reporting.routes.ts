import { Routes } from '@angular/router';

// RF-RE: dashboard (landing) + un reporte por ruta. Segmentos en español, consistentes con el resto.
export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./dashboard/reporting-dashboard.component').then((m) => m.ReportingDashboardComponent),
  },
  {
    path: 'puestos-autorizados',
    loadComponent: () =>
      import('./authorized-posts/authorized-posts-report.component').then(
        (m) => m.AuthorizedPostsReportComponent,
      ),
  },
  {
    path: 'dotacion',
    loadComponent: () =>
      import('./staffing-complement/staffing-complement-report.component').then(
        (m) => m.StaffingComplementReportComponent,
      ),
  },
  {
    path: 'costo-estimado',
    loadComponent: () =>
      import('./estimated-cost/estimated-cost-report.component').then(
        (m) => m.EstimatedCostReportComponent,
      ),
  },
  {
    path: 'comparativo',
    loadComponent: () =>
      import('./comparative/comparative-report.component').then((m) => m.ComparativeReportComponent),
  },
  {
    path: 'cambios-estructurales',
    loadComponent: () =>
      import('./structural-changes/structural-changes-report.component').then(
        (m) => m.StructuralChangesReportComponent,
      ),
  },
];
