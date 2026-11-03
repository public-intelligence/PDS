/** Catálogos territoriales de la República Dominicana (RF-LO). */
export interface Pais {
  id: string;
  codigo: string;
  nombre: string;
}

export interface Region {
  id: string;
  codigo: string;
  nombre: string;
  paisId: string;
}

export interface Provincia {
  id: string;
  codigo: string;
  nombre: string;
  paisId: string;
  regionId?: string;
}

export interface Municipio {
  id: string;
  codigo: string;
  nombre: string;
  provinciaId: string;
}

export interface Distrito {
  id: string;
  codigo: string;
  nombre: string;
  municipioId: string;
}
