import { Component, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { Router } from '@angular/router';
import { DriverService } from '../../../../core/services/driver.service';
import { DriverSummary, OnboardingStatus } from '../../../../core/models/driver.model';
import { MessageService } from 'primeng/api';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-drivers-list',
  imports: [
    TableModule,
    ButtonModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './drivers-list.component.html',
  styleUrl: './drivers-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriversListComponent {
  private readonly driverService = inject(DriverService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  drivers = signal<DriverSummary[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);

  selectedStatus = signal<OnboardingStatus>('UnderReview');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(10);

  statuses: OnboardingStatus[] = ['Incomplete', 'UnderReview', 'Approved', 'Rejected', 'Suspended'];

  query = computed(() => ({
    onboardingStatus: this.selectedStatus(),
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize()
  }));

  constructor() {
    toObservable(this.query).pipe(
      tap(() => this.loading.set(true)),
      switchMap(query =>
        this.driverService.getAll(query).pipe(
          catchError(() => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to fetch drivers'
            });
            this.loading.set(false);
            return of(null);
          })
        )
      )
    ).subscribe(response => {
      if (!response) return;

      this.drivers.set(response.items);
      this.totalRecords.set(response.totalCount);
      this.loading.set(false);
    });
  }

  onStatusChange(status: OnboardingStatus): void {
    this.selectedStatus.set(status);
    this.pageNumber.set(1);
  }

  onPageChange(event: TableLazyLoadEvent): void {
    this.pageNumber.set((event.first! / event.rows!) + 1);
    this.pageSize.set(event.rows!);
  }

  viewDetails(driver: DriverSummary): void {
    this.router.navigate(['/dashboard/drivers', driver.id]);
  }

  getStatusSeverity(status: OnboardingStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    const severityMap: Record<OnboardingStatus, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
      'Incomplete': 'warn',
      'UnderReview': 'info',
      'Approved': 'success',
      'Rejected': 'danger',
      'Suspended': 'secondary'
    };
    return severityMap[status];
  }
}
