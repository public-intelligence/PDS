/** Resultado paginado leído de las cabeceras X-* de la respuesta (RF-LO-15). */
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
