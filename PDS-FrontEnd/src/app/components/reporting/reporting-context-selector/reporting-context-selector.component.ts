import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { StructureVersionService } from '../../../services/structure-version/structure-version.service';
import { StructureVersionSummary, versionLabel } from '../../../models/structure-version.model';
import {
  ReportingContext,
  ReportingContextKind,
  contextLabel,
} from '../../../models/reporting/reporting-context.model';

/**
 * Selector de contexto de reportería compartido (RF-RE): versión vigente / versión publicada
 * específica / a una fecha. Emite un ReportingContext. Refleja la lógica mutuamente excluyente del
 * visor de organigrama.
 */
@Component({
  selector: 'app-reporting-context-selector',
  template: `
    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-xs text-slate-600">
        <span>Contexto del reporte</span>
        <select
          class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
          [disabled]="disabled()"
          [value]="mode()"
          (change)="onModeChange($any($event.target).value)"
        >
          <option value="current">Versión vigente</option>
          <option value="version">Versión publicada específica</option>
          <option value="asOf">A una fecha</option>
        </select>
      </label>

      @if (mode() === 'version') {
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Versión</span>
          <select
            class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
            [disabled]="disabled() || loading()"
            [value]="selectedVersionId()"
            (change)="onVersionChange($any($event.target).value)"
          >
            <option value="">Seleccione…</option>
            @for (v of versions(); track v.id) {
              <option [value]="v.id">{{ label(v) }} — {{ v.name }}</option>
            }
          </select>
        </label>
      }

      @if (mode() === 'asOf') {
        <label class="flex flex-col gap-1 text-xs text-slate-600">
          <span>Fecha de corte</span>
          <input
            type="date"
            class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500"
            [disabled]="disabled()"
            [value]="selectedDate()"
            (change)="onDateChange($any($event.target).value)"
          />
        </label>
      }

      <span class="rounded-full bg-blue-50 px-3 py-1 text-xs font-medium text-primary-blue">
        Contexto: {{ resolvedLabel() }}
      </span>
    </div>
  `,
})
export class ReportingContextSelectorComponent implements OnInit {
  private versionService = inject(StructureVersionService);

  readonly value = input<ReportingContext>({});
  readonly disabled = input(false);
  readonly contextChange = output<ReportingContext>();

  protected mode = signal<ReportingContextKind>('current');
  protected versions = signal<StructureVersionSummary[]>([]);
  protected selectedVersionId = signal('');
  protected selectedDate = signal('');
  protected loading = signal(true);

  protected resolvedLabel = computed(() => contextLabel(this.currentContext(), this.versions()));

  ngOnInit(): void {
    const v = this.value();
    if (v.versionId) {
      this.mode.set('version');
      this.selectedVersionId.set(v.versionId);
    } else if (v.asOfDate) {
      this.mode.set('asOf');
      this.selectedDate.set(v.asOfDate);
    }
    this.loadVersions();
  }

  private async loadVersions(): Promise<void> {
    try {
      const items = await firstValueFrom(this.versionService.list());
      // Versiones materializadas (publicadas o históricas) que tienen número asignado.
      this.versions.set(items.filter((x) => x.number != null));
    } catch {
      this.versions.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected label(v: StructureVersionSummary): string {
    return versionLabel(v);
  }

  protected onModeChange(mode: string): void {
    this.mode.set(mode as ReportingContextKind);
    this.emit();
  }

  protected onVersionChange(id: string): void {
    this.selectedVersionId.set(id);
    this.emit();
  }

  protected onDateChange(date: string): void {
    this.selectedDate.set(date);
    this.emit();
  }

  private currentContext(): ReportingContext {
    if (this.mode() === 'version') return { versionId: this.selectedVersionId() || undefined };
    if (this.mode() === 'asOf') return { asOfDate: this.selectedDate() || undefined };
    return {};
  }

  private emit(): void {
    this.contextChange.emit(this.currentContext());
  }
}
