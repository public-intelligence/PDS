import { Component, computed, input, output, signal } from '@angular/core';
import { DIAS_SEMANA, DiaEfectivo } from '../../../models/locality-calendar.model';
import { MONTH_NAMES, monthGrid } from '../../../utils/calendar-dates';
import { DAY_KINDS, DAY_KIND_LABELS, DayKind, dayKind } from '../../../utils/calendar-labels';
import { formatIsoDate } from '../../../utils/locality-time';
import { DAY_INITIAL, formatIntervals } from '../../../utils/schedule';

interface DayCell {
  date: string;
  dayOfMonth: number;
  kind: DayKind | null;
  tooltip: string;
}

/**
 * Vista anual del calendario efectivo (UI-05 §5): doce meses con el estado de cada día. Cada
 * estado combina color y trama, y la leyenda los nombra, para no depender solo del color (RNF-LO-10).
 */
@Component({
  selector: 'app-calendar-annual-view',
  templateUrl: './calendar-annual-view.component.html',
  styleUrl: './calendar-annual-view.component.css',
})
export class CalendarAnnualViewComponent {
  readonly year = input.required<number>();
  readonly days = input<DiaEfectivo[]>([]);
  /** Fecha local de la localidad: marca "hoy" (RF-LO-23). */
  readonly today = input('');
  readonly canManage = input(false);
  readonly addException = output<string>();

  protected readonly weekdayInitials = DIAS_SEMANA.map((day) => DAY_INITIAL[day]);
  protected readonly legend = DAY_KINDS.map((kind) => ({ kind, label: DAY_KIND_LABELS[kind] }));
  protected selectedDate = signal<string | null>(null);

  private dayByDate = computed(() => new Map(this.days().map((d) => [d.fecha.slice(0, 10), d])));

  protected months = computed(() =>
    MONTH_NAMES.map((name, month) => ({
      name,
      weeks: monthGrid(this.year(), month).map((week) =>
        week.map((date) => (date ? this.toCell(date) : null)),
      ),
    })),
  );

  protected selected = computed(() => {
    const date = this.selectedDate();
    return date ? { date, day: this.dayByDate().get(date) } : null;
  });

  protected select(date: string): void {
    this.selectedDate.set(this.selectedDate() === date ? null : date);
  }

  protected formatDate(date: string): string {
    return formatIsoDate(date);
  }

  protected hours(day: DiaEfectivo): string {
    return formatIntervals(day.intervalos);
  }

  protected kindLabel(day: DiaEfectivo): string {
    return DAY_KIND_LABELS[dayKind(day)];
  }

  private toCell(date: string): DayCell {
    const day = this.dayByDate().get(date);
    return {
      date,
      dayOfMonth: Number(date.slice(8, 10)),
      kind: day ? dayKind(day) : null,
      tooltip: day ? this.tooltip(day) : formatIsoDate(date),
    };
  }

  private tooltip(day: DiaEfectivo): string {
    const status = day.laborable ? `Laborable ${formatIntervals(day.intervalos)}` : 'No laborable';
    const reasons = (day.motivos ?? []).map((m) => m.nombre).join(', ');
    return [formatIsoDate(day.fecha), status.trim(), reasons].filter(Boolean).join(' · ');
  }
}
