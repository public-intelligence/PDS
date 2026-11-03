import { Injectable } from '@angular/core';
import { jsPDF } from 'jspdf';
import { saveAs } from 'file-saver';

/**
 * Exporta el organigrama (un nodo <svg> en vivo) a SVG vectorial, PNG y PDF.
 * Como el visor dibuja un SVG nativo, el SVG es vectorial verdadero; el PNG se rasteriza del SVG
 * y el PDF incrusta el PNG.
 */
@Injectable({ providedIn: 'root' })
export class OrgChartExportService {
  /** Exporta el SVG tal cual (vectorial). */
  exportSvg(svg: SVGSVGElement, filename = 'organigrama.svg'): void {
    const source = this.serialize(svg);
    const blob = new Blob([source], { type: 'image/svg+xml;charset=utf-8' });
    saveAs(blob, filename);
  }

  /** Rasteriza el SVG a PNG (escala por densidad de pantalla, hasta 3x). */
  async exportPng(svg: SVGSVGElement, filename = 'organigrama.png'): Promise<void> {
    const { width, height } = this.dimensions(svg);
    const scale = Math.min(Math.max(window.devicePixelRatio || 1, 2), 3);
    const canvas = await this.rasterize(svg, width, height, scale);
    canvas.toBlob((blob) => {
      if (blob) saveAs(blob, filename);
    }, 'image/png');
  }

  /** Incrusta el organigrama (PNG) en un PDF con orientación según su relación de aspecto. */
  async exportPdf(svg: SVGSVGElement, filename = 'organigrama.pdf', title?: string): Promise<void> {
    const { width, height } = this.dimensions(svg);
    const scale = Math.min(Math.max(window.devicePixelRatio || 1, 2), 3);
    const canvas = await this.rasterize(svg, width, height, scale);
    const imgData = canvas.toDataURL('image/png');

    const orientation = width >= height ? 'landscape' : 'portrait';
    const pdf = new jsPDF({ orientation, unit: 'pt', format: 'a4' });
    const pageWidth = pdf.internal.pageSize.getWidth();
    const pageHeight = pdf.internal.pageSize.getHeight();
    const margin = 24;

    let titleSpace = 0;
    if (title) {
      pdf.setFontSize(13);
      pdf.text(title, margin, margin + 6);
      pdf.setFontSize(9);
      pdf.text(`Generado: ${new Date().toLocaleString('es-DO')}`, margin, margin + 22);
      titleSpace = 36;
    }

    const availWidth = pageWidth - margin * 2;
    const availHeight = pageHeight - margin * 2 - titleSpace;
    const ratio = Math.min(availWidth / width, availHeight / height);
    const imgWidth = width * ratio;
    const imgHeight = height * ratio;
    const x = (pageWidth - imgWidth) / 2;
    const y = margin + titleSpace;

    pdf.addImage(imgData, 'PNG', x, y, imgWidth, imgHeight);
    pdf.save(filename);
  }

  // ───────────────────────── helpers ─────────────────────────

  private serialize(svg: SVGSVGElement): string {
    const clone = svg.cloneNode(true) as SVGSVGElement;
    // El zoom/pan se aplica como transform CSS en el <svg> en vivo; al exportar
    // debe ignorarse para obtener el organigrama completo a tamaño natural.
    clone.style.removeProperty('transform');
    clone.style.removeProperty('transform-origin');
    clone.style.removeProperty('cursor'); // irrelevante en la imagen, se limpia por prolijidad
    // El visor posiciona el SVG en absoluto dentro del lienzo; al exportar es un SVG raíz independiente.
    clone.style.removeProperty('position');
    clone.style.removeProperty('top');
    clone.style.removeProperty('left');
    const { width, height } = this.dimensions(svg);

    clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
    clone.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
    clone.setAttribute('width', `${width}`);
    clone.setAttribute('height', `${height}`);

    // Fondo blanco (un SVG transparente se rasteriza negro en algunos motores).
    const bg = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
    bg.setAttribute('x', '0');
    bg.setAttribute('y', '0');
    bg.setAttribute('width', '100%');
    bg.setAttribute('height', '100%');
    bg.setAttribute('fill', '#ffffff');
    clone.insertBefore(bg, clone.firstChild);

    return new XMLSerializer().serializeToString(clone);
  }

  private dimensions(svg: SVGSVGElement): { width: number; height: number } {
    const viewBox = svg.getAttribute('viewBox');
    if (viewBox) {
      const parts = viewBox.split(/\s+/).map(Number);
      if (parts.length === 4 && parts[2] > 0 && parts[3] > 0) {
        return { width: parts[2], height: parts[3] };
      }
    }
    const rect = svg.getBoundingClientRect();
    return { width: rect.width || 1200, height: rect.height || 800 };
  }

  private rasterize(
    svg: SVGSVGElement,
    width: number,
    height: number,
    scale: number,
  ): Promise<HTMLCanvasElement> {
    const source = this.serialize(svg);
    const url = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(source)}`;

    return new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = Math.ceil(width * scale);
        canvas.height = Math.ceil(height * scale);
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('No se pudo crear el contexto del canvas.'));
          return;
        }
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.scale(scale, scale);
        ctx.drawImage(img, 0, 0, width, height);
        resolve(canvas);
      };
      img.onerror = () => reject(new Error('No se pudo rasterizar el organigrama.'));
      img.src = url;
    });
  }
}
