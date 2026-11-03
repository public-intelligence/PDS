export type ChartType = 'bar' | 'donut';

export interface ChartDatum {
  label: string;
  value: number;
  color?: string;
}
