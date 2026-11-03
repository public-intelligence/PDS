export type StatusNotification = 'success' | 'error' | 'warning' | 'default';

export interface Notification {
  message: string;
  duration?: number;
  status: StatusNotification;
}
