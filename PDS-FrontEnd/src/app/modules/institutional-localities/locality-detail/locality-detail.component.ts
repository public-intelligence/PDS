import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LayoutService } from '@public-intelligence/ui-core';

import { AuditLogPanelComponent } from '../../../components/ui/audit-log-panel/audit-log-panel.component';
import { AuditLog } from '../../../models/audit-log.model';
import { NotificationService } from '../../../services/notification/notification.service';
import { InstitutionalLocalityService } from '../../../services/institutional-locality/institutional-locality.service';
import { EstadoLocalidadService } from '../../../services/locality-catalog/locality-catalog.services';
import { LocalityDetail, LocalityStatusCode } from '../../../models/institutional-locality.model';
import { MapHandle, MapProvider, RD_CENTER, RD_ZOOM } from '../../../services/map/map-provider';
import { LeafletMapProvider } from '../../../services/map/leaflet-map.provider';
import { LocalityFormModalComponent } from '../../../components/ui/modals/locality-form-modal/locality-form-modal.component';
import { LocalitySuspendModalComponent } from '../../../components/ui/modals/locality-suspend-modal/locality-suspend-modal.component';
import { LocalityCloseModalComponent } from '../../../components/ui/modals/locality-close-modal/locality-close-modal.component';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal.component';
import { AuthService } from '../../../services/auth/auth.service';
import { LocalityCalendarService } from '../../../services/locality-calendar/locality-calendar.service';
import { CANAL_LABELS } from '../../../utils/calendar-labels';
import { isFeatureEnabled } from '../../../utils/feature-flags';
import { isAbroad } from '../../../utils/locality-format';
import { formatLocalityTime, timeZoneAbbreviation } from '../../../utils/locality-time';
import { injectNow } from '../../../utils/now-signal';
import { LocalityCalendarCardComponent } from '../locality-calendar/locality-calendar-card.component';

/** Vista de detalle consolidada (RF-LO-14): datos, ubicación, cobertura, servicios, responsables,
 *  documentos, instrumentos legales, mini-mapa y línea de tiempo de cambios. */
@Component({
  selector: 'app-locality-detail',
  imports: [
    RouterLink,
    AuditLogPanelComponent,
    ConfirmationModalComponent,
    LocalityCalendarCardComponent,
    LocalityFormModalComponent,
    LocalitySuspendModalComponent,
    LocalityCloseModalComponent,
  ],
  providers: [{ provide: MapProvider, useClass: LeafletMapProvider }],
  templateUrl: './locality-detail.component.html',
})
export class LocalityDetailComponent implements OnInit, AfterViewInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(InstitutionalLocalityService);
  private estadoService = inject(EstadoLocalidadService);
  private layoutService = inject(LayoutService);
  private notification = inject(NotificationService);
  private mapProvider = inject(MapProvider);
  private calendarService = inject(LocalityCalendarService);
  private auth = inject(AuthService);
  private now = injectNow();

  @ViewChild(ConfirmationModalComponent) confirmationModal!: ConfirmationModalComponent;

  protected readonly showCalendar = isFeatureEnabled('calendarioLaboral');
  protected readonly showFunciones = isFeatureEnabled('funcionesLocalidad');
  private readonly isCalendarAdmin = this.auth.isCalendarAdmin();

  private mapContainer = viewChild<ElementRef<HTMLElement>>('miniMap');
  private mapHandle?: MapHandle;
  private mapReady = false;

  protected isLoading = signal(true);
  protected locality = signal<LocalityDetail | null>(null);
  private estadoCodeById = new Map<string, LocalityStatusCode>();

  // Modales
  protected isFormOpen = signal(false);
  protected isSuspendOpen = signal(false);
  protected suspendMode = signal<'suspend' | 'reactivate'>('suspend');
  protected isCloseOpen = signal(false);

  protected auditLogs = computed<AuditLog[]>(() =>
    (this.locality()?.historial ?? []).map((h) => ({
      id: h.id,
      action: h.accion,
      userId: '',
      userName: h.usuario,
      date: h.fecha,
      comment: h.motivo,
      channel: h.canal ? (CANAL_LABELS[h.canal] ?? h.canal) : null,
    })),
  );

  protected isAbroad = computed(() => {
    const l = this.locality();
    return !!l && isAbroad(l);
  });
  /** `08:41 (EDT)`, en la zona de la localidad y refrescada cada minuto (§12.2). */
  protected localTime = computed(() => {
    const zone = this.locality()?.zonaHoraria;
    if (!zone) return '';
    return `${formatLocalityTime(this.now(), zone)} (${timeZoneAbbreviation(zone, this.now())})`;
  });
  /** RF-LO-46: la estructura de una representación vive en su unidad vinculada. */
  protected missingLinkedUnit = computed(() => {
    const l = this.locality();
    return !!l?.funciones?.length && !l.unidadVinculadaId;
  });
  /** Solo el administrador elige la localidad predeterminada de calendario (RF-LO-36). */
  protected canSetDefault = computed(
    () =>
      this.showCalendar &&
      this.isCalendarAdmin &&
      !!this.locality() &&
      !this.locality()!.esLocalidadCalendarioPredeterminada,
  );

  protected statusCode = computed<LocalityStatusCode | undefined>(() => {
    const l = this.locality();
    return l ? this.estadoCodeById.get(l.estadoLocalidadId) : undefined;
  });
  protected isSuspended = computed(() => this.statusCode() === 'SUSPENDIDA_TEMP');
  protected isClosed = computed(
    () => this.statusCode() === 'EN_CIERRE' || this.statusCode() === 'INACTIVA',
  );

  ngOnInit(): void {
    this.layoutService.setPageTitle('Detalle de localidad');
    this.layoutService.breadcrumbs.set([
      { label: 'Localidades', url: '/localidades' },
      { label: 'Detalle', url: '' },
    ]);
    void this.loadEstados();
    void this.load();
  }

  async ngAfterViewInit(): Promise<void> {
    const el = this.mapContainer()?.nativeElement;
    if (!el) return;
    this.mapHandle = await this.mapProvider.init({
      container: el,
      center: RD_CENTER,
      zoom: RD_ZOOM,
      interactive: true,
    });
    this.mapReady = true;
    this.placeMarker();
  }

  ngOnDestroy(): void {
    this.mapHandle?.destroy();
  }

  private async loadEstados(): Promise<void> {
    try {
      const result = await firstValueFrom(this.estadoService.getAll());
      this.estadoCodeById = new Map(result.items.map((e) => [e.id, e.codigo as LocalityStatusCode]));
    } catch {
      /* los botones de acción se ocultan con gracia si no hay catálogo */
    }
  }

  /** `silent` refresca los datos sin desmontar la página (p. ej. tras un cambio de calendario). */
  protected async load(silent = false): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    if (!silent) this.isLoading.set(true);
    try {
      const detail = await firstValueFrom(this.service.detail(id));
      this.locality.set(detail);
      this.layoutService.setPageTitle(detail.nombreOficial);
      this.placeMarker();
    } catch {
      this.notification.error('No se pudo cargar la localidad');
    } finally {
      this.isLoading.set(false);
    }
  }

  private placeMarker(): void {
    const l = this.locality();
    if (!this.mapReady || !l || l.latitud == null || l.longitud == null) return;
    this.mapProvider.setView([l.latitud, l.longitud], 14);
    this.mapProvider.setSingleMarker({ lat: l.latitud, lng: l.longitud });
  }

  protected hasCoords(): boolean {
    const l = this.locality();
    return !!l && l.latitud != null && l.longitud != null;
  }

  // ── Acciones ──
  protected openEdit(): void {
    this.isFormOpen.set(true);
  }
  protected openSuspend(mode: 'suspend' | 'reactivate'): void {
    this.suspendMode.set(mode);
    this.isSuspendOpen.set(true);
  }
  protected openClose(): void {
    this.isCloseOpen.set(true);
  }
  protected onChanged(): void {
    this.isFormOpen.set(false);
    this.isSuspendOpen.set(false);
    this.isCloseOpen.set(false);
    void this.load();
  }
  protected confirmSetDefault(): void {
    const l = this.locality();
    if (!l) return;
    this.confirmationModal.openModal({
      title: 'Localidad predeterminada de calendario',
      name: l.nombreOficial,
      paragraph: 'Marcar como localidad predeterminada de calendario a',
      warning: 'La localidad predeterminada actual dejará de serlo.',
      confirmButton: 'Marcar como predeterminada',
      onConfirm: () => this.setDefault(l.id),
    });
  }

  private setDefault(id: string): void {
    this.confirmationModal.closeModal();
    this.calendarService.marcarPredeterminada(id).subscribe({
      next: () => {
        this.notification.success('Localidad predeterminada de calendario actualizada');
        void this.load(true);
      },
      error: (err) => {
        this.notification.fromHttpError(err, 'No se pudo marcar como predeterminada');
        void this.load(true);
      },
    });
  }

  protected goBack(): void {
    this.router.navigate(['/localidades']);
  }
}
