import { Component, inject, signal, ChangeDetectionStrategy, computed, effect } from '@angular/core';
import { Router } from '@angular/router';
import { DriverService } from '../../../../core/services/driver.service';
import { DriverSummary, OnboardingStatus } from '../../../../core/models/driver.model';
import { MessageService } from 'primeng/api';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { DriversPageStore } from './drivers-store';

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
  // private readonly driverService = inject(DriverService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  statuses: OnboardingStatus[] = ['Incomplete', 'UnderReview', 'Approved', 'Rejected', 'Suspended'];

  protected readonly store = inject(DriversPageStore);

  constructor() {
    this.store.loadByQuery(this.store.query);

    effect(() => {
      console.log(`is loading: ${this.store.loading()}`);
    })
  }

  onStatusChange(status: OnboardingStatus): void {
    this.store.updateStatus(status);
  }

  onPageChange(event: TableLazyLoadEvent): void {
    this.store.updatePage(event);
  }

  viewDetails(driver: DriverSummary): void {
    this.router.navigate(['/dashboard/drivers', driver.id]);
  }
}
