import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AuditLog, VERSION_ACTION_LABELS, VersionAction } from '../../../models/audit-log.model';

@Component({
  selector: 'app-audit-log-panel',
  imports: [DatePipe],
  templateUrl: './audit-log-panel.component.html',
})
export class AuditLogPanelComponent {
  readonly logs = input<AuditLog[]>([]);

  protected label(action: VersionAction): string {
    return VERSION_ACTION_LABELS[action] ?? action;
  }

  protected dotColor(action: VersionAction): string {
    switch (action) {
      case 'Published':
      case 'Reactivated':
        return 'bg-green-500';
      case 'Approved':
        return 'bg-blue-500';
      case 'Rejected':
      case 'Closed':
        return 'bg-red-500';
      case 'Superseded':
        return 'bg-slate-400';
      case 'Suspended':
        return 'bg-amber-500';
      case 'Modified':
        return 'bg-indigo-500';
      default:
        return 'bg-amber-500';
    }
  }
}
