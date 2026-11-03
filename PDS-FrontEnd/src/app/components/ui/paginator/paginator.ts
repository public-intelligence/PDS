import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgxPaginationModule } from 'ngx-pagination';

@Component({
  selector: 'app-paginator',
  imports: [NgClass, FormsModule, NgxPaginationModule],
  templateUrl: './paginator.html',
})
export class Paginator implements OnInit, OnChanges {
  @Input() pageNumber = 1;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Input() pageSizeInput = 10;
  @Output() pageToEmit = new EventEmitter<number>();
  @Output() pageSize = new EventEmitter<number>();

  protected currentSize = signal<number>(10);
  protected pagaSizeRange: number[] = [];
  protected dummyArray = [1];

  ngOnInit(): void {
    this.currentSize.set(this.pageSizeInput || 20);
    this.sizeRange();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['pageSizeInput'] && !changes['pageSizeInput'].firstChange) {
      this.currentSize.set(this.pageSizeInput || 10);
    }
  }

  private sizeRange(): void {
    this.pagaSizeRange = [5, 10, 20, 50, 100];
  }

  emitPageSize(size: number): void {
    const parsedSize = Number(size);
    this.currentSize.set(parsedSize);
    this.pageSize.emit(parsedSize);
    this.pageToEmit.emit(1);
  }

  onPageChange(page: number): void {
    this.pageToEmit.emit(page);
  }
}
