import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-crud-page',
  templateUrl: './crud-page.layout.html',
})
export class CrudPageLayout {
  @Input() showFilters: boolean = true;
  @Input() showActions: boolean = true;
  @Input() showPagination: boolean = true;
}
