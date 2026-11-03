import { Component, ViewChild, computed, effect, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';

import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import {
  CalendarioLocalidad,
  DIAS_SEMANA,
  DiaEfectivo,
  EstadoCalendario,
  ExcepcionCalendario,
  JornadaVersion,
  SuscripcionCalendario,
  ZonaHorariaActual,
} from '../../../models/locality-calendar.model';
import { AuthService } from '../../../services/auth/auth.service';
import { LocalityCalendarService } from '../../../services/locality-calendar/locality-calendar.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { isBeforeRetroWindow, yearRange } from '../../../utils/calendar-dates';
import {
  CANAL_LABELS,
  CATEGORIA_EXCEPCION_LABELS,
  ORIGEN_EXCEPCION_LABELS,
  TIPO_EXCEPCION_LABELS,
} from '../../../utils/calendar-labels';
import {
  formatIsoDate,
  formatLocalityTime,
  formatTransition,
  localityToday,
  timeZoneAbbreviation,
} from '../../../utils/locality-time';
import { injectNow } from '../../../utils/now-signal';
import {
  DAY_LONG,
  DAY_SHORT,
  formatIntervals,
  normalizeWeek,
  scheduleTimeline,
  summarizeSchedule,
} from '../../../utils/schedule';
import { formatUtcOffset } from '../../../utils/time-zone';
import { CalendarAnnualViewComponent } from './calendar-annual-view.component';
import { ExceptionEditorModalComponent } from './exception-editor-modal.component';
import { ScheduleEditorModalComponent } from './schedule-editor-modal.component';
import { SubscriptionsModalComponent } from './subscriptions-modal.component';

const DEFAULT_RETRO_WINDOW_DAYS = 7;
const SLA_WARNING = 'Los plazos de las solicitudes en curso se recalcularán.';

/**
 * Tarjeta «Calendario laboral» del detalle de la localidad (UI-05): zona horaria, horario semanal,
 * calendarios de feriados, días no laborables y vista anual. Todos leen; editan los gestores de
 * calendario de la localidad y los administradores (RF-LO-38).
 */
@Component({
  selector: 'app-locality-calendar-card',
  imports: [
    RouterLink,
    ConfirmationModalComponent,
    CalendarAnnualViewComponent,
    ExceptionEditorModalComponent,
    ScheduleEditorModalComponent,
    SubscriptionsModalComponent,
  ],
  templateUrl: './locality-calendar-card.component.html',
})
export class LocalityCalendarCardComponent {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private service = inject(LocalityCalendarService);
  private auth = inject(AuthService);
  private notification = inject(NotificationService);
  private now = injectNow();

  readonly localityId = input.required<string>();
  readonly localityName = input('');
  readonly paisId = input<string | null>(null);
  /** Un cambio del calendario también cambia la bitácora y la versión de la localidad. */
  readonly changed = output<void>();

  protected readonly isAdmin = this.auth.isCalendarAdmin();
  protected readonly weekdays = DIAS_SEMANA;
  protected readonly dayLong = DAY_LONG;
  protected readonly dayShort = DAY_SHORT;

  protected isLoading = signal(true);
  protected hasError = signal(false);
  protected calendario = signal<CalendarioLocalidad | null>(null);
  protected zona = signal<ZonaHorariaActual | null>(null);
  protected estado = signal<EstadoCalendario | null>(null);

  protected year = signal(new Date().getFullYear());
  protected isLoadingYear = signal(false);
  protected yearError = signal(false);
  protected excepciones = signal<ExcepcionCalendario[]>([]);
  protected diasEfectivos = signal<DiaEfectivo[]>([]);

  protected showHistory = signal(false);
  protected previewedVersionId = signal<string | null>(null);

  // Modales
  protected isScheduleOpen = signal(false);
  protected isSubscriptionsOpen = signal(false);
  protected isExceptionOpen = signal(false);
  protected editingException = signal<ExcepcionCalendario | null>(null);
  protected exceptionDate = signal('');

  // ── Estado derivado ──
  protected canManage = computed(() => this.isAdmin || !!this.calendario()?.puedeGestionar);
  protected retroWindowDays = computed(
    () => this.calendario()?.ventanaRetroactivaDias ?? DEFAULT_RETRO_WINDOW_DAYS,
  );
  /** "Hoy" es el de la localidad, no el del navegador (RF-LO-23). */
  protected today = computed(() => {
    const zone = this.calendario()?.zonaHoraria;
    return zone ? localityToday(zone, this.now()) : '';
  });
  protected timeline = computed(() =>
    scheduleTimeline(this.calendario()?.jornadas ?? [], this.today()),
  );
  protected currentWeek = computed(() => {
    const current = this.timeline().current;
    return current ? normalizeWeek(current.dias) : null;
  });
  protected localTime = computed(() => {
    const zone = this.calendario()?.zonaHoraria;
    if (!zone) return '';
    const abbreviation = this.zona()?.abreviatura ?? timeZoneAbbreviation(zone, this.now());
    return `${formatLocalityTime(this.now(), zone)} (${abbreviation})`;
  });
  protected offset = computed(() => {
    const zona = this.zona();
    return zona ? formatUtcOffset(zona.desplazamientoUtc) : '';
  });
  protected nextClockChange = computed(() => {
    const zona = this.zona();
    const next = zona?.proximaTransicion;
    if (!zona) return '';
    if (!next) return 'Sin cambios de hora';
    const when = formatTransition(next.instante, zona.desplazamientoMinutos);
    return `Próximo cambio de hora: ${when} → ${formatUtcOffset(next.desplazamientoUtc)}`;
  });

  constructor() {
    effect(() => {
      this.localityId();
      void this.load();
    });
  }

  // ── Carga ──
  protected async load(): Promise<void> {
    const id = this.localityId();
    this.isLoading.set(true);
    this.hasError.set(false);
    try {
      this.calendario.set(await firstValueFrom(this.service.calendario(id)));
      // La franja de cabecera es informativa: si falla, la tarjeta sigue siendo útil.
      const [zona, estado] = await Promise.all([
        firstValueFrom(this.service.zonaHoraria(id)).catch(() => null),
        firstValueFrom(this.service.estado(id)).catch(() => null),
      ]);
      this.zona.set(zona);
      this.estado.set(estado);
      void this.loadYear();
    } catch {
      this.hasError.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async loadYear(): Promise<void> {
    const id = this.localityId();
    const { desde, hasta } = yearRange(this.year());
    this.isLoadingYear.set(true);
    this.yearError.set(false);
    try {
      const [excepciones, efectivo] = await Promise.all([
        firstValueFrom(this.service.excepciones(id, desde, hasta)),
        firstValueFrom(this.service.efectivo(id, desde, hasta)),
      ]);
      this.excepciones.set(
        [...excepciones].sort((a, b) => a.fechaInicio.localeCompare(b.fechaInicio)),
      );
      this.diasEfectivos.set(efectivo.dias);
    } catch {
      this.yearError.set(true);
    } finally {
      this.isLoadingYear.set(false);
    }
  }

  protected setYear(year: number): void {
    this.year.set(year);
    void this.loadYear();
  }

  /** Tras cualquier cambio: recarga la tarjeta y avisa al detalle (bitácora, versión). */
  protected onSaved(): void {
    this.isScheduleOpen.set(false);
    this.isSubscriptionsOpen.set(false);
    this.isExceptionOpen.set(false);
    this.changed.emit();
    void this.load();
  }

  // ── Presentación ──
  protected hours(version: JornadaVersion, day: (typeof DIAS_SEMANA)[number]): string {
    return formatIntervals(normalizeWeek(version.dias)[day]);
  }
  /** Intervalos de un día, uno por línea en la vista previa de una versión programada. */
  protected dayIntervals(version: JornadaVersion, day: (typeof DIAS_SEMANA)[number]): string[] {
    return normalizeWeek(version.dias)[day].map((i) => `${i.inicio}–${i.fin}`);
  }
  protected summary(version: JornadaVersion): string {
    return summarizeSchedule(normalizeWeek(version.dias));
  }
  protected date(value?: string | null): string {
    return formatIsoDate(value);
  }
  protected subscriptionLabel(s: SuscripcionCalendario): string {
    const validity =
      s.vigenteDesde || s.vigenteHasta
        ? ` (${s.vigenteDesde ? this.date(s.vigenteDesde) : '…'} – ${s.vigenteHasta ? this.date(s.vigenteHasta) : '…'})`
        : '';
    return `${s.codigo} · ${s.nombre}${validity}`;
  }
  protected exceptionDates(e: ExcepcionCalendario): string {
    const start = this.date(e.fechaInicio);
    if (!e.fechaFin) return `Desde ${start}`;
    const end = this.date(e.fechaFin);
    return start === end ? start : `${start} – ${end}`;
  }
  protected exceptionHours(e: ExcepcionCalendario): string {
    return formatIntervals(e.intervalos) || '—';
  }
  protected tipoLabel(e: ExcepcionCalendario): string {
    return TIPO_EXCEPCION_LABELS[e.tipo] ?? e.tipo;
  }
  protected categoriaLabel(e: ExcepcionCalendario): string {
    return CATEGORIA_EXCEPCION_LABELS[e.categoria] ?? e.categoria;
  }
  protected origenLabel(e: ExcepcionCalendario): string {
    return e.origen ? (ORIGEN_EXCEPCION_LABELS[e.origen] ?? e.origen) : '—';
  }
  protected canalLabel(e: ExcepcionCalendario): string {
    return e.canal ? (CANAL_LABELS[e.canal] ?? e.canal) : '—';
  }

  /** Motivo por el que una fila no admite acciones; vacío si se puede editar (UI-05 §3). */
  protected lockReason(e: ExcepcionCalendario): string {
    if (e.esSistema) {
      const cause = e.categoria === 'CierreDefinitivo' ? 'el cierre' : 'la suspensión';
      return `Generado por ${cause} de la localidad`;
    }
    const tooOld = isBeforeRetroWindow(e.fechaInicio, this.today(), this.retroWindowDays());
    return !this.isAdmin && tooOld
      ? `Fuera del plazo para cambios retroactivos (${this.retroWindowDays()} días).`
      : '';
  }

  // ── Horario ──
  protected togglePreview(version: JornadaVersion): void {
    this.previewedVersionId.set(this.previewedVersionId() === version.id ? null : version.id);
  }

  protected confirmDeleteVersion(version: JornadaVersion): void {
    this.confirmationModal.openModal({
      title: 'Eliminar horario programado',
      name: this.date(version.vigenteDesde),
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar el horario programado desde el',
      warning: SLA_WARNING,
      confirmButton: 'Eliminar',
      onConfirm: () =>
        this.runDelete(
          this.service.eliminarJornada(this.localityId(), version.id),
          'Horario programado eliminado',
        ),
    });
  }

  // ── Días no laborables ──
  protected openCreateException(date = ''): void {
    this.editingException.set(null);
    this.exceptionDate.set(date);
    this.isExceptionOpen.set(true);
  }

  protected openEditException(e: ExcepcionCalendario): void {
    this.editingException.set(e);
    this.isExceptionOpen.set(true);
  }

  protected confirmDeleteException(e: ExcepcionCalendario): void {
    this.confirmationModal.openModal({
      title: 'Eliminar día no laborable',
      name: `${this.date(e.fechaInicio)} al ${this.date(e.fechaFin ?? e.fechaInicio)}`,
      isDelete: true,
      paragraph: 'Eliminar el día no laborable del',
      warning: SLA_WARNING,
      confirmButton: 'Eliminar',
      onConfirm: () =>
        this.runDelete(
          this.service.eliminarExcepcion(this.localityId(), e.id),
          'Día no laborable eliminado',
        ),
    });
  }

  private runDelete(request$: Observable<void>, done: string): void {
    this.confirmationModal.closeModal();
    request$.subscribe({
      next: () => {
        this.notification.success(done);
        this.onSaved();
      },
      // El servidor es la autoridad: se muestra su mensaje y se refresca (§12.2).
      error: (err) => {
        this.notification.fromHttpError(err, 'No se pudo eliminar');
        void this.load();
      },
    });
  }
}
