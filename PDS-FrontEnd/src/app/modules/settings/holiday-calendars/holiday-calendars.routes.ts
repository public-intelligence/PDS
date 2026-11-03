import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./holiday-calendars-list.component').then((m) => m.HolidayCalendarsListComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./holiday-calendar-detail.component').then((m) => m.HolidayCalendarDetailComponent),
  },
];
