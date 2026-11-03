export type VersionAction =
  | 'Created'
  | 'SubmittedForReview'
  | 'Approved'
  | 'Rejected'
  | 'Published'
  | 'Superseded'
  // Acciones de la bitácora de localidades institucionales (RF-LO-06/14).
  | 'Modified'
  | 'Suspended'
  | 'Reactivated'
  | 'Closed'
  // Cambios del calendario laboral (RF-LO-44).
  | 'ScheduleChanged'
  | 'HolidaySubscriptionsChanged'
  | 'CalendarExceptionAdded'
  | 'CalendarExceptionUpdated'
  | 'CalendarExceptionRemoved';

export const VERSION_ACTION_LABELS: Record<VersionAction, string> = {
  Created: 'Creada',
  SubmittedForReview: 'Enviada a revisión',
  Approved: 'Aprobada',
  Rejected: 'Rechazada',
  Published: 'Publicada',
  Superseded: 'Reemplazada',
  Modified: 'Modificada',
  Suspended: 'Suspendida',
  Reactivated: 'Reactivada',
  Closed: 'Cerrada',
  ScheduleChanged: 'Horario laboral modificado',
  HolidaySubscriptionsChanged: 'Calendarios de feriados modificados',
  CalendarExceptionAdded: 'Día no laborable agregado',
  CalendarExceptionUpdated: 'Día no laborable modificado',
  CalendarExceptionRemoved: 'Día no laborable eliminado',
};

export interface AuditLog {
  id: string;
  action: VersionAction;
  userId: string;
  userName?: string | null;
  date: string;
  comment?: string | null;
  /** Origen del cambio, ya traducido (p. ej. «PDS», «Intranet / API externa»). */
  channel?: string | null;
}
