import { Injectable } from '@angular/core';
import { MapPoint } from '../../models/institutional-locality.model';
import {
  MapColorMode,
  MapHandle,
  MapInitOptions,
  MapProvider,
  PickResult,
  SetMarkersOptions,
} from './map-provider';
import { environment } from '../../../environments/environment';
import { formatLocalityTime, timeZoneAbbreviation } from '../../utils/locality-time';

type LeafletModule = typeof import('leaflet');

const FALLBACK_COLOR = '#003876';
const TILE_URL =
  (environment as { mapTileUrl?: string }).mapTileUrl ??
  'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
const ATTRIBUTION =
  (environment as { mapAttribution?: string }).mapAttribution ??
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';
const CLUSTER_THRESHOLD = 100;
/** Cada cuánto se refresca la «Hora local» del popup abierto (UI-02). */
const POPUP_REFRESH_MS = 60_000;

/**
 * Implementación de {@link MapProvider} con Leaflet + OpenStreetMap. Leaflet se carga de forma
 * perezosa (dynamic import) para mantenerlo fuera del bundle inicial.
 */
@Injectable()
export class LeafletMapProvider extends MapProvider {
  private L!: LeafletModule;
  private map: import('leaflet').Map | null = null;
  private markersLayer: import('leaflet').LayerGroup | null = null;
  private pickMarker: import('leaflet').Marker | null = null;
  private markerClickHandler?: (id: string) => void;
  private pickHandler?: (p: PickResult) => void;
  private resizeObserver: ResizeObserver | null = null;
  private popupTimer?: ReturnType<typeof setInterval>;

  async init(opts: MapInitOptions): Promise<MapHandle> {
    this.L = await import('leaflet');
    await import('leaflet.markercluster');
    this.fixDefaultIconPaths();

    this.map = this.L.map(opts.container, {
      center: opts.center,
      zoom: opts.zoom,
      zoomControl: opts.interactive !== false,
      dragging: opts.interactive !== false,
      scrollWheelZoom: opts.interactive !== false,
      attributionControl: opts.showAttribution !== false,
    });
    this.L.tileLayer(TILE_URL, {
      attribution: ATTRIBUTION,
      maxZoom: 20,
      subdomains: 'abcd',
      detectRetina: true,
    }).addTo(this.map);

    // El contenedor puede no tener tamaño final al crearse (modal con @if, paneles flex,
    // mini-mapa de detalle). Recalculamos tras el primer layout y observamos resizes.
    requestAnimationFrame(() => requestAnimationFrame(() => this.map?.invalidateSize()));
    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => this.map?.invalidateSize());
      this.resizeObserver.observe(opts.container);
    }

    this.popupTimer = setInterval(() => this.refreshOpenPopup(), POPUP_REFRESH_MS);

    return {
      destroy: () => {
        clearInterval(this.popupTimer);
        this.resizeObserver?.disconnect();
        this.resizeObserver = null;
        this.disablePickMode();
        this.map?.remove();
        this.map = null;
        this.markersLayer = null;
        this.pickMarker = null;
      },
    };
  }

  setMarkers(points: MapPoint[], opts?: SetMarkersOptions): void {
    if (!this.map) return;
    if (this.markersLayer) {
      this.map.removeLayer(this.markersLayer);
      this.markersLayer = null;
    }

    const useCluster = opts?.cluster ?? points.length > CLUSTER_THRESHOLD;
    const group = useCluster
      ? (
          this.L as unknown as {
            markerClusterGroup: (o?: unknown) => import('leaflet').LayerGroup;
          }
        ).markerClusterGroup({
          showCoverageOnHover: false,
          iconCreateFunction: (cluster: { getChildCount: () => number }) =>
            this.clusterIcon(cluster.getChildCount()),
        })
      : this.L.layerGroup();

    for (const p of points) {
      const color = this.colorFor(p, opts?.colorMode ?? 'state');
      const marker = this.L.marker([p.latitud, p.longitud], { icon: this.pinIcon(color) });
      // Como función, el contenido se recalcula al abrir y al refrescar: la hora local no caduca.
      marker.bindPopup(() => this.popupHtml(p));
      marker.bindTooltip(this.escape(p.nombreOficial), {
        permanent: true,
        direction: 'top',
        offset: [0, -16],
        className: 'locality-label',
      });
      marker.on('click', () => this.markerClickHandler?.(p.id));
      group.addLayer(marker);
    }
    group.addTo(this.map);
    this.markersLayer = group;
  }

  onMarkerClick(handler: (id: string) => void): void {
    this.markerClickHandler = handler;
  }

  fitBounds(points: { latitud: number; longitud: number }[], padding = 40): void {
    if (!this.map || points.length === 0) return;
    const bounds = this.L.latLngBounds(points.map((p) => [p.latitud, p.longitud] as [number, number]));
    if (bounds.isValid()) this.map.fitBounds(bounds, { padding: [padding, padding], maxZoom: 15 });
  }

  setView(center: [number, number], zoom: number): void {
    this.map?.setView(center, zoom);
  }

  setSingleMarker(p: PickResult): void {
    if (!this.map) return;
    if (this.pickMarker) {
      this.pickMarker.setLatLng([p.lat, p.lng]);
    } else {
      this.pickMarker = this.L.marker([p.lat, p.lng], { draggable: true }).addTo(this.map);
      this.pickMarker.on('dragend', () => {
        const ll = this.pickMarker!.getLatLng();
        this.pickHandler?.({ lat: ll.lat, lng: ll.lng });
      });
    }
  }

  enablePickMode(handler: (p: PickResult) => void): void {
    if (!this.map) return;
    this.pickHandler = handler;
    this.map.on('click', this.onPickClick);
  }

  disablePickMode(): void {
    this.map?.off('click', this.onPickClick);
    this.pickHandler = undefined;
  }

  private onPickClick = (e: import('leaflet').LeafletMouseEvent): void => {
    const { lat, lng } = e.latlng;
    this.setSingleMarker({ lat, lng });
    this.pickHandler?.({ lat, lng });
  };

  private colorFor(p: MapPoint, mode: MapColorMode): string {
    const c = mode === 'type' ? p.tipoColor : p.estadoColor;
    return c || FALLBACK_COLOR;
  }

  private clusterIcon(count: number): import('leaflet').DivIcon {
    const size = count < 10 ? 34 : count < 100 ? 40 : 48;
    return this.L.divIcon({
      className: 'locality-cluster',
      html: `<span style="width:${size}px;height:${size}px">${count}</span>`,
      iconSize: [size, size],
    });
  }

  private pinIcon(color: string): import('leaflet').DivIcon {
    return this.L.divIcon({
      className: 'locality-pin',
      html:
        `<span style="display:block;width:16px;height:16px;border-radius:50% 50% 50% 0;` +
        `transform:rotate(-45deg);background:${color};border:2px solid white;` +
        `box-shadow:0 0 3px rgba(0,0,0,.45)"></span>`,
      iconSize: [16, 16],
      iconAnchor: [8, 16],
      popupAnchor: [0, -14],
    });
  }

  private refreshOpenPopup(): void {
    this.markersLayer?.eachLayer((layer) => {
      const popup = layer.getPopup();
      if (popup?.isOpen()) popup.update();
    });
  }

  private popupHtml(p: MapPoint): string {
    const rows = [
      `<strong>${this.escape(p.nombreOficial)}</strong>`,
      p.tipoNombre ? `Tipo: ${this.escape(p.tipoNombre)}` : '',
      p.estadoNombre ? `Estado: ${this.escape(p.estadoNombre)}` : '',
      p.unidadResponsableNombre ? `Unidad: ${this.escape(p.unidadResponsableNombre)}` : '',
      p.direccionCompleta ? `${this.escape(p.direccionCompleta)}` : '',
      p.paisNombre ? `País: ${this.escape(p.paisNombre)}` : '',
      p.ciudad ? `Ciudad: ${this.escape(p.ciudad)}` : '',
      ...(p.zonaHoraria ? this.timeZoneRows(p.zonaHoraria) : []),
    ].filter(Boolean);
    return `<div style="font-size:12px;line-height:1.4">${rows.join('<br>')}</div>`;
  }

  private timeZoneRows(timeZone: string): string[] {
    const now = new Date();
    const localTime = `${formatLocalityTime(now, timeZone)} (${timeZoneAbbreviation(timeZone, now)})`;
    return [`Zona horaria: ${this.escape(timeZone)}`, `Hora local: ${this.escape(localTime)}`];
  }

  private escape(s: string): string {
    return s.replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]!);
  }

  /** Corrige las rutas de los iconos por defecto (servidos en /assets/leaflet vía angular.json). */
  private fixDefaultIconPaths(): void {
    const proto = this.L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown };
    delete proto._getIconUrl;
    this.L.Icon.Default.mergeOptions({
      iconRetinaUrl: 'assets/leaflet/marker-icon-2x.png',
      iconUrl: 'assets/leaflet/marker-icon.png',
      shadowUrl: 'assets/leaflet/marker-shadow.png',
    });
  }
}
