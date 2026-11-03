import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { InstitutionalLocalityService } from '../../../services/institutional-locality/institutional-locality.service';
import { LocalityListQuery, MapPoint } from '../../../models/institutional-locality.model';
import {
  MapColorMode,
  MapHandle,
  MapProvider,
  RD_CENTER,
  RD_ZOOM,
} from '../../../services/map/map-provider';
import { LeafletMapProvider } from '../../../services/map/leaflet-map.provider';
import { Router } from '@angular/router';

/** Vista de mapa (RF-LO-16). Reutilizable como página o embebida (toggle de la lista). */
@Component({
  selector: 'app-localities-map',
  templateUrl: './localities-map.component.html',
  providers: [{ provide: MapProvider, useClass: LeafletMapProvider }],
})
export class LocalitiesMapComponent implements AfterViewInit, OnDestroy {
  readonly query = input<LocalityListQuery>({});
  readonly interactive = input(true);

  private container = viewChild.required<ElementRef<HTMLElement>>('mapContainer');
  private provider = inject(MapProvider);
  private service = inject(InstitutionalLocalityService);
  private router = inject(Router);

  protected colorMode = signal<MapColorMode>('state');
  protected points = signal<MapPoint[]>([]);
  private handle?: MapHandle;
  private ready = false;

  /** Categorías visibles para la leyenda, según el modo de color y los puntos cargados. */
  protected legend = computed(() => {
    const mode = this.colorMode();
    const groups = new Map<string, { label: string; color: string; count: number }>();
    for (const p of this.points()) {
      const label = (mode === 'type' ? p.tipoNombre : p.estadoNombre) ?? 'Sin clasificar';
      const color = (mode === 'type' ? p.tipoColor : p.estadoColor) ?? '#003876';
      const entry = groups.get(label);
      if (entry) entry.count++;
      else groups.set(label, { label, color, count: 1 });
    }
    return [...groups.values()].sort((a, b) => b.count - a.count);
  });

  constructor() {
    // Recarga marcadores cuando cambian los filtros entrantes.
    effect(() => {
      const q = this.query();
      if (this.ready) void this.reload(q);
    });
  }

  async ngAfterViewInit(): Promise<void> {
    this.handle = await this.provider.init({
      container: this.container().nativeElement,
      center: RD_CENTER,
      zoom: RD_ZOOM,
      interactive: this.interactive(),
    });
    this.provider.onMarkerClick((id) => this.router.navigate(['/localidades', id]));
    this.ready = true;
    await this.reload(this.query());
  }

  ngOnDestroy(): void {
    this.handle?.destroy();
  }

  protected setColorMode(mode: MapColorMode): void {
    this.colorMode.set(mode);
    void this.reload(this.query());
  }

  /** Reencuadra el mapa a los marcadores cargados. */
  protected fit(): void {
    this.provider.fitBounds(this.points());
  }

  private async reload(query: LocalityListQuery): Promise<void> {
    try {
      const points = await firstValueFrom(this.service.mapa(query));
      this.points.set(points);
      this.provider.setMarkers(points, { colorMode: this.colorMode(), cluster: points.length > 100 });
      this.provider.fitBounds(points);
    } catch {
      // El mapa queda vacío; los errores de red ya se notifican en otras vistas.
      this.points.set([]);
    }
  }
}
