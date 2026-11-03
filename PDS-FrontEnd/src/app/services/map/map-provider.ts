import { InjectionToken } from '@angular/core';
import { MapPoint } from '../../models/institutional-locality.model';

/** Manija de un mapa inicializado; libera recursos al destruir el componente. */
export interface MapHandle {
  destroy(): void;
}

export interface MapInitOptions {
  container: HTMLElement;
  center: [number, number];
  zoom: number;
  interactive?: boolean;
  /** Muestra el control de atribución de Leaflet. Por defecto `true`; el modal lo desactiva. */
  showAttribution?: boolean;
}

export type MapColorMode = 'type' | 'state';

export interface SetMarkersOptions {
  colorMode?: MapColorMode;
  cluster?: boolean;
}

export interface PickResult {
  lat: number;
  lng: number;
}

/**
 * Abstracción del proveedor de mapas (RF-LO-16). La implementación por defecto usa Leaflet +
 * OpenStreetMap; se puede sustituir por Mapbox/Google cambiando un solo binding de DI.
 */
export abstract class MapProvider {
  abstract init(opts: MapInitOptions): Promise<MapHandle>;
  abstract setMarkers(points: MapPoint[], opts?: SetMarkersOptions): void;
  abstract onMarkerClick(handler: (id: string) => void): void;
  abstract fitBounds(points: { latitud: number; longitud: number }[], padding?: number): void;
  abstract setView(center: [number, number], zoom: number): void;
  abstract setSingleMarker(p: PickResult): void;
  abstract enablePickMode(handler: (p: PickResult) => void): void;
  abstract disablePickMode(): void;
}

export const MAP_PROVIDER = new InjectionToken<MapProvider>('MAP_PROVIDER');

/** Centro aproximado de la República Dominicana. */
export const RD_CENTER: [number, number] = [18.7357, -70.1627];
export const RD_ZOOM = 8;
