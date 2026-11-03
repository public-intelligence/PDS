import { HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { saveAs } from 'file-saver';
import { Observable, firstValueFrom } from 'rxjs';
import { NotificationService } from '../notification/notification.service';
import { filenameFromContentDisposition } from '../../utils/content-disposition';

/**
 * Descarga un blob generado por el servidor: resuelve el nombre desde Content-Disposition (con
 * respaldo) y lo guarda con file-saver. Centraliza el manejo de errores de exportación (RF-RE-06).
 */
@Injectable({ providedIn: 'root' })
export class ReportDownloadService {
  private notification = inject(NotificationService);

  async download(source: Observable<HttpResponse<Blob>>, fallbackName: string): Promise<void> {
    try {
      const response = await firstValueFrom(source);
      const name = filenameFromContentDisposition(
        response.headers.get('Content-Disposition'),
        fallbackName,
      );
      if (response.body) saveAs(response.body, name);
    } catch (e) {
      this.notification.fromHttpError(e, 'No se pudo generar el reporte.');
    }
  }
}
