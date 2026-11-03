import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';

import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { HolidayCalendarModalComponent } from '../../../components/ui/modals/holiday-calendar-modal/holiday-calendar-modal.component';
import { HolidayEntryModalComponent } from '../../../components/ui/modals/holiday-entry-modal/holiday-entry-modal.component';
import { HolidayImportModalComponent } from '../../../components/ui/modals/holiday-import-modal/holiday-import-modal.component';
import { CalendarioFeriados, Feriado } from '../../../models/holiday-calendar.model';
import { AuthService } from '../../../services/auth/auth.service';
import { HolidayCalendarService } from '../../../services/holiday-calendar/holiday-calendar.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { ALCANCE_LABELS, AMBITO_LABELS } from '../../../utils/calendar-labels';
import { formatDateWithWeekday, isBeforeRetroWindow } from '../../../utils/calendar-dates';
import { formatIsoDate, localityToday } from '../../../utils/locality-time';
import { formatIntervals } from '../../../utils/schedule';

type Tab = 'feriados' | 'suscriptores';
const DEFAULT_RETRO_WINDOW_DAYS = 7;

/** UI-10: detalle de un calendario de feriados con sus días y localidades suscritas. */
@Component({
  selector: 'app-holiday-calendar-detail',
  imports: [
    RouterLink,
    ConfirmationModalComponent,
    HolidayCalendarModalComponent,
    HolidayEntryModalComponent,
    HolidayImportModalComponent,
  ],
  templateUrl: './holiday-calendar-detail.component.html',
})
export class HolidayCalendarDetailComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(HolidayCalendarService);
  private auth = inject(AuthService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);

  private readonly id = this.route.snapshot.paramMap.get('id') ?? '';
  /** Un calendario compartido no tiene zona propia: "hoy" es el del navegador. */
  protected readonly today = localityToday(Intl.DateTimeFormat().resolvedOptions().timeZone);

  protected readonly isAdmin = this.auth.isCalendarAdmin();
  protected readonly ambitoLabels = AMBITO_LABELS;
  protected readonly currentYear = new Date().getFullYear();

  protected isLoading = signal(true);
  protected hasError = signal(false);
  protected calendario = signal<CalendarioFeriados | null>(null);
  protected tab = signal<Tab>('feriados');
  protected year = signal(this.currentYear);
  protected feriados = signal<Feriado[]>([]);
  protected isLoadingFeriados = signal(false);
  protected feriadosError = signal(false);

  protected isCalendarModalOpen = signal(false);
  protected isEntryModalOpen = signal(false);
  protected isImportModalOpen = signal(false);
  protected editingFeriado = signal<Feriado | null>(null);

  /** Administradores y gestores de la localidad gestora mantienen los feriados (RF-LO-38). */
  protected canEdit = computed(() => this.isAdmin || !!this.calendario()?.puedeGestionar);
  protected suscriptores = computed(() => this.calendario()?.suscriptores ?? []);
  protected retroWindowDays = computed(
    () => this.calendario()?.ventanaRetroactivaDias ?? DEFAULT_RETRO_WINDOW_DAYS,
  );
  protected defaultEntryDate = computed(() =>
    this.year() === this.currentYear ? this.today : `${this.year()}-01-01`,
  );

  ngOnInit(): void {
    this.layoutService.setPageTitle('Calendario de feriados');
    this.layoutService.breadcrumbs.set([
      { label: 'Calendarios de feriados', url: '/settings/calendarios-feriados' },
      { label: 'Detalle', url: '' },
    ]);
    void this.load();
  }

  protected async load(): Promise<void> {
    this.isLoading.set(true);
    this.hasError.set(false);
    try {
      const calendario = await firstValueFrom(this.service.getById(this.id));
      this.calendario.set(calendario);
      this.layoutService.setPageTitle(`${calendario.codigo} · ${calendario.nombre}`);
      void this.loadFeriados();
    } catch {
      this.hasError.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async loadFeriados(): Promise<void> {
    this.isLoadingFeriados.set(true);
    this.feriadosError.set(false);
    try {
      const feriados = await firstValueFrom(this.service.dias(this.id, this.year()));
      this.feriados.set([...feriados].sort((a, b) => a.fecha.localeCompare(b.fecha)));
    } catch {
      this.feriadosError.set(true);
    } finally {
      this.isLoadingFeriados.set(false);
    }
  }

  protected setYear(year: number): void {
    this.year.set(year);
    void this.loadFeriados();
  }

  // ── Presentación ──
  protected fecha(f: Feriado): string {
    return formatDateWithWeekday(f.fecha);
  }

  protected alcance(f: Feriado): string {
    const label = ALCANCE_LABELS[f.alcance] ?? f.alcance;
    return f.alcance === 'JornadaReducida' ? `${label} ${formatIntervals(f.intervalos)}` : label;
  }

  protected vigencia(desde?: string | null, hasta?: string | null): string {
    if (!desde && !hasta) return 'Sin límite';
    return `${desde ? formatIsoDate(desde) : '…'} – ${hasta ? formatIsoDate(hasta) : '…'}`;
  }

  /** Un gestor no toca feriados anteriores a la ventana retroactiva (RF-LO-33). */
  protected isLocked(f: Feriado): boolean {
    return !this.isAdmin && isBeforeRetroWindow(f.fecha, this.today, this.retroWindowDays());
  }

  // ── Acciones ──
  protected openCreate(): void {
    this.editingFeriado.set(null);
    this.isEntryModalOpen.set(true);
  }

  protected openEdit(f: Feriado): void {
    this.editingFeriado.set(f);
    this.isEntryModalOpen.set(true);
  }

  protected onEntrySaved(): void {
    this.isEntryModalOpen.set(false);
    void this.load();
  }

  protected onImported(): void {
    this.isImportModalOpen.set(false);
    void this.load();
  }

  protected onCalendarModalClosed(): void {
    this.isCalendarModalOpen.set(false);
    void this.load();
  }

  protected confirmDelete(f: Feriado): void {
    const affected = this.calendario()?.localidadesSuscritas ?? this.suscriptores().length;
    this.confirmationModal.openModal({
      title: 'Eliminar feriado',
      name: `${f.nombre} (${formatIsoDate(f.fecha)})`,
      isDelete: true,
      paragraph: 'Está seguro que desea eliminar el feriado',
      warning: `Afecta a ${affected} localidades. Los plazos de las solicitudes en curso se recalcularán.`,
      confirmButton: 'Eliminar',
      onConfirm: () => this.delete(f.id),
    });
  }

  private delete(diaId: string): void {
    this.confirmationModal.closeModal();
    this.service.eliminarDia(this.id, diaId).subscribe({
      next: () => {
        this.notification.success('Feriado eliminado');
        void this.load();
      },
      error: (err) => {
        this.notification.fromHttpError(err, 'No se pudo eliminar el feriado');
        void this.load();
      },
    });
  }

  protected goBack(): void {
    void this.router.navigate(['/settings/calendarios-feriados']);
  }
}
