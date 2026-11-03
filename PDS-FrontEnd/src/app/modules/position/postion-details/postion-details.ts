import { Component, inject, OnInit, signal } from '@angular/core';
import { PositionService } from '../../../services/position/position.service';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../../../services/notification/notification.service';
import { Position } from '../../../models/position.model';
import { LayoutService } from '@public-intelligence/ui-core';
import { PdfService } from '../../../services/pdf/pdf.service';
import { PositionPdfBuilder } from '../../../services/pdf/position-pdf.builder';

@Component({
  selector: 'app-postion-details',
  imports: [],
  templateUrl: './postion-details.html',
})
export class PostionDetails implements OnInit {
  private positionService = inject(PositionService);
  protected isLoading = signal(false);
  protected position = signal<Position | null>(null);

  private route = inject(ActivatedRoute);
  private id = this.route.snapshot.paramMap.get('id');
  private notification = inject(NotificationService);
  private layoutService = inject(LayoutService);
  private pdfService = inject(PdfService);
  private positionPdfBuilder = inject(PositionPdfBuilder);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Detalle de cargo');
    this.layoutService.breadcrumbs.set([
      { label: 'Cargos', url: 'positions' },
      { label: 'Detalle de cargo', url: `positions/details/${this.id}` },
    ]);
    this.getPositionById();
  }

  async getPositionById() {
    try {
      this.isLoading.set(true);
      const position = await firstValueFrom(this.positionService.getById(this.id!));
      this.position.set(position);
    } catch (error) {
      this.notification.error('Error al cargar el cargo');
    } finally {
      this.isLoading.set(false);
    }
  }

  downloadPdf(): void {
    const position = this.position();
    if (!position) return;
    const doc = this.positionPdfBuilder.build(position);
    this.pdfService.download(doc, position.name);
  }

  splitItems(value?: string): string[] {
    return (value ?? '')
      .split('\x1F')
      .map((s) => s.trim())
      .filter(Boolean);
  }
}
