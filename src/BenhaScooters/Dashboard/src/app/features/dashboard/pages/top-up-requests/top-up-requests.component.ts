import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { MessageService } from 'primeng/api';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ToastModule } from 'primeng/toast';
import {
  TopUpRequest,
  TopUpRequestStatus
} from '../../../../core/models/wallet-topup.model';
import { WalletTopUpService } from '../../../../core/services/wallet-topup.service';

@Component({
  selector: 'app-top-up-requests',
  imports: [
    TableModule,
    ButtonModule,
    DialogModule,
    Textarea,
    SelectButtonModule,
    ToastModule,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule
  ],
  providers: [MessageService],
  templateUrl: './top-up-requests.component.html',
  styleUrl: './top-up-requests.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopUpRequestsComponent {
  private readonly walletTopUpService = inject(WalletTopUpService);
  private readonly messageService = inject(MessageService);
  private readonly formBuilder = inject(FormBuilder);

  readonly statuses: Array<TopUpRequestStatus | 'All'> = ['All', 'Pending', 'Approved', 'Rejected'];
  readonly requests = signal<TopUpRequest[]>([]);
  readonly selectedStatus = signal<TopUpRequestStatus | 'All'>('Pending');
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalRecords = signal(0);
  readonly loading = signal(false);
  readonly refreshTrigger = signal(0);

  readonly showReviewDialog = signal(false);
  readonly selectedRequest = signal<TopUpRequest | null>(null);
  readonly reviewSubmitting = signal(false);

  readonly reviewForm = this.formBuilder.group({
    approve: this.formBuilder.nonNullable.control(true),
    note: this.formBuilder.nonNullable.control('')
  });

  readonly reviewOptions: Array<{ label: string; value: boolean }> = [
    { label: 'Approve', value: true },
    { label: 'Reject', value: false }
  ];

  readonly query = computed(() => {
    const status = this.selectedStatus();

    return {
      page: this.pageNumber(),
      pageSize: this.pageSize(),
      status: status === 'All' ? undefined : status,
      refresh: this.refreshTrigger()
    };
  });

  constructor() {
    this.reviewForm.controls.approve.valueChanges.subscribe((approve) => {
      const noteControl = this.reviewForm.controls.note;

      if (!approve) {
        noteControl.setValidators([Validators.required]);
      } else {
        noteControl.clearValidators();
      }

      noteControl.updateValueAndValidity({ emitEvent: false });
    });

    toObservable(this.query)
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(({ refresh: _refresh, ...query }) =>
          this.walletTopUpService.getRequests(query).pipe(
            catchError(() => {
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to load top-up requests'
              });
              this.loading.set(false);
              return of(null);
            })
          )
        )
      )
      .subscribe((response) => {
        if (!response) return;

        this.requests.set(response.items);
        this.totalRecords.set(response.totalCount);
        this.loading.set(false);
      });
  }

  onStatusChange(status: TopUpRequestStatus | 'All'): void {
    this.selectedStatus.set(status);
    this.pageNumber.set(1);
  }

  onPageChange(event: TableLazyLoadEvent): void {
    this.pageNumber.set((event.first! / event.rows!) + 1);
    this.pageSize.set(event.rows!);
  }

  openReviewDialog(request: TopUpRequest): void {
    this.selectedRequest.set(request);
    this.reviewForm.reset({ approve: true, note: '' });
    this.showReviewDialog.set(true);
  }

  closeReviewDialog(): void {
    this.showReviewDialog.set(false);
    this.selectedRequest.set(null);
    this.reviewSubmitting.set(false);
  }

  onDialogVisibleChange(visible: boolean): void {
    if (visible) {
      this.showReviewDialog.set(true);
      return;
    }

    this.closeReviewDialog();
  }

  submitReview(): void {
    const request = this.selectedRequest();
    if (!request) return;

    this.reviewForm.markAllAsTouched();

    if (this.reviewForm.invalid) {
      return;
    }

    const approve = this.reviewForm.controls.approve.value;
    const noteValue = this.reviewForm.controls.note.value.trim();

    this.reviewSubmitting.set(true);
    this.walletTopUpService
      .review(request.id, {
        approve,
        note: noteValue || undefined
      })
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: approve ? 'Top-up request approved' : 'Top-up request rejected'
          });

          this.closeReviewDialog();
          this.refreshTrigger.update((value) => value + 1);
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to review top-up request'
          });
          this.reviewSubmitting.set(false);
        }
      });
  }

  openReceipt(url: string): void {
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  getStatusSeverity(status: TopUpRequestStatus): 'success' | 'info' | 'warn' | 'danger' {
    const severityMap: Record<TopUpRequestStatus, 'success' | 'info' | 'warn' | 'danger'> = {
      Pending: 'warn',
      Approved: 'success',
      Rejected: 'danger'
    };

    return severityMap[status];
  }
}
