import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { DriverService } from '../../../../core/services/driver.service';
import { DriverDetails, OnboardingStatus } from '../../../../core/models/driver.model';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { DatePipe, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-driver-details',
  imports: [
    ButtonModule,
    ToastModule,
    CardModule,
    DialogModule,
    Textarea,
    DatePipe,
    CommonModule,
    FormsModule
  ],
  providers: [MessageService],
  templateUrl: './driver-details.component.html',
  styleUrl: './driver-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DriverDetailsComponent implements OnInit {
  private readonly driverService = inject(DriverService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  driver = signal<DriverDetails | null>(null);
  driverId: number | null = null;
  loading = signal<boolean>(true);
  actionLoading = signal<boolean>(false);
  showBanModal = signal<boolean>(false);
  banReason = signal<string>('');

  ngOnInit(): void {
    const driverId = this.route.snapshot.paramMap.get('id');
    if (driverId) {
      this.driverId = Number(driverId);
      this.loadDriverDetails(this.driverId);
    }
  }

  private loadDriverDetails(id: number): void {
    this.loading.set(true);
    this.driverService.getById(id).subscribe({
      next: (driver) => {
        console.log('Fetched driver details:', driver);
        this.driver.set(driver);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to fetch driver details'
        });
        this.loading.set(false);
      }
    });
  }

  onApprove(): void {
    const driver = this.driver();
    if (!driver) return;

    this.actionLoading.set(true);
    this.driverService.approve(this.driverId!).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Driver approved successfully'
        });
        setTimeout(() => this.router.navigate(['/dashboard/drivers']), 1500);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to approve driver'
        });
        this.actionLoading.set(false);
      }
    });
  }

  onBanClick(): void {
    this.showBanModal.set(true);
    this.banReason.set('');
  }

  onBanConfirm(): void {
    const driver = this.driver();
    const reason = this.banReason();

    if (!driver || !reason.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please provide a reason for banning'
      });
      return;
    }

    this.actionLoading.set(true);
    this.driverService.ban(this.driverId!, reason).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Driver banned successfully'
        });
        this.showBanModal.set(false);
        setTimeout(() => this.router.navigate(['/dashboard/drivers']), 1500);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to ban driver'
        });
        this.actionLoading.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard/drivers']);
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
