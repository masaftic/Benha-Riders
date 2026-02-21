import { Component, inject, signal, ChangeDetectionStrategy, OnInit, computed } from '@angular/core';
import { Router } from '@angular/router';
import { ParentService } from '../../../../core/services/parent.service';
import { ParentSummary } from '../../../../core/models/parent.model';
import { MessageService } from 'primeng/api';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { DatePipe } from '@angular/common';
import { debounceSignal } from '../../../../core/utils';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-parents-list',
  imports: [
    TableModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    DatePipe
  ],
  providers: [MessageService],
  templateUrl: './parents-list.component.html',
  styleUrl: './parents-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ParentsList implements OnInit {
  private readonly parentService = inject(ParentService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  parents = signal<ParentSummary[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);

  searchInputVal = signal<string>('');
  searchQuery = debounceSignal(this.searchInputVal, 500, '');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(10);
  orderBy = signal<string>('joinedAt');
  isDescending = signal<boolean>(true);

  query = computed(() => ({
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize(),
    search: this.searchQuery(),
    orderBy: this.orderBy(),
    isDescending: this.isDescending()
  }));

  constructor() {
    toObservable(this.query).pipe(
      tap(() => this.loading.set(true)),
      switchMap(query =>
        this.parentService.getAll(query).pipe(
          catchError(() => {
            this.loading.set(false);
            return of(null);
          })
        )
      )
    ).subscribe(response => {
      if (!response) return;

      this.parents.set(response.items);
      this.totalRecords.set(response.totalCount);
      this.loading.set(false);
    });
  }

  ngOnInit(): void {}

  onGlobalFilter(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInputVal.set(value);
    this.pageNumber.set(1);
  }

  onPageChange(event: TableLazyLoadEvent): void {
    this.pageNumber.set((event.first! / event.rows!) + 1);
    this.pageSize.set(event.rows!);
  }

  onSort(event: { field: string, order: number }): void {
    this.orderBy.set(event.field);
    this.isDescending.set(event.order === -1);
  }

  viewDetails(parent: ParentSummary): void {
    this.router.navigate(['/dashboard/parents-list', parent.id]);
  }
}
