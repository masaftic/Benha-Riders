import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { DriverDetails } from '../../models/driver.models';

@Component({
  selector: 'app-driver-details',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="driver-details">
      <div class="header">
        <h1>Driver Details</h1>
        <button class="btn btn-outline" (click)="backToList()">
          ← Back to List
        </button>
      </div>

      @if (loading()) {
        <div class="loading">Loading driver details...</div>
      } @else if (driver()) {
        <div class="content">
          <div class="section">
            <h2>Status Information</h2>
            <div class="info-grid">
              <div class="info-item">
                <label>Status:</label>
                <span [class]="'badge badge-' + getStatusClass(driver()!.onboardingStatus)">
                  {{ driver()!.onboardingStatus }}
                </span>
              </div>
              <div class="info-item">
                <label>Created:</label>
                <span>{{ formatDate(driver()!.createdAt) }}</span>
              </div>
              @if (driver()!.approvedAt) {
                <div class="info-item">
                  <label>Approved:</label>
                  <span>{{ formatDate(driver()!.approvedAt!) }}</span>
                </div>
              }
              @if (driver()!.rejectionReason) {
                <div class="info-item full-width">
                  <label>Rejection Reason:</label>
                  <span class="rejection-reason">{{ driver()!.rejectionReason }}</span>
                </div>
              }
            </div>
          </div>

          @if (driver()!.personalInfo) {
            <div class="section">
              <h2>Personal Information</h2>
              <div class="info-grid">
                <div class="info-item">
                  <label>Full Name:</label>
                  <span>{{ driver()!.personalInfo!.fullName }}</span>
                </div>
                <div class="info-item">
                  <label>National ID:</label>
                  <span>{{ driver()!.personalInfo!.nationalId }}</span>
                </div>
                <div class="info-item">
                  <label>Date of Birth:</label>
                  <span>{{ formatDate(driver()!.personalInfo!.dateOfBirth!) }}</span>
                </div>
                <div class="info-item">
                  <label>City:</label>
                  <span>{{ driver()!.personalInfo!.city }}</span>
                </div>
                <div class="info-item full-width">
                  <label>Address:</label>
                  <span>{{ driver()!.personalInfo!.address }}</span>
                </div>
                <div class="info-item">
                  <label>Emergency Contact:</label>
                  <span>{{ driver()!.personalInfo!.emergencyContactName }}</span>
                </div>
                <div class="info-item">
                  <label>Emergency Phone:</label>
                  <span>{{ driver()!.personalInfo!.emergencyContactPhone }}</span>
                </div>
              </div>
            </div>
          }

          @if (driver()!.vehicle) {
            <div class="section">
              <h2>Vehicle Information</h2>
              <div class="info-grid">
                <div class="info-item">
                  <label>Type:</label>
                  <span>{{ driver()!.vehicle!.vehicleType }}</span>
                </div>
                <div class="info-item">
                  <label>Brand:</label>
                  <span>{{ driver()!.vehicle!.brand }}</span>
                </div>
                <div class="info-item">
                  <label>Model:</label>
                  <span>{{ driver()!.vehicle!.model }}</span>
                </div>
                <div class="info-item">
                  <label>Year:</label>
                  <span>{{ driver()!.vehicle!.year }}</span>
                </div>
                <div class="info-item">
                  <label>Color:</label>
                  <span>{{ driver()!.vehicle!.color }}</span>
                </div>
                <div class="info-item">
                  <label>License Plate:</label>
                  <span>{{ driver()!.vehicle!.licensePlate }}</span>
                </div>
                <div class="info-item">
                  <label>VIN:</label>
                  <span>{{ driver()!.vehicle!.vin }}</span>
                </div>
              </div>
            </div>
          }

          @if (driver()!.documents.length > 0) {
            <div class="section">
              <h2>Documents</h2>
              <div class="documents-grid">
                @for (doc of driver()!.documents; track doc.documentType) {
                  <div class="document-card">
                    <h3>{{ doc.documentType }}</h3>
                    <img [src]="doc.imageUrl" [alt]="doc.documentType" />
                    <div class="document-info">
                      <small>Uploaded: {{ formatDate(doc.uploadedAt) }}</small>
                      @if (doc.expiryDate) {
                        <small>Expires: {{ formatDate(doc.expiryDate!) }}</small>
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          }

          <div class="actions">
            @if (driver()!.onboardingStatus === 'Pending') {
              <button class="btn btn-success" (click)="approve()" [disabled]="actionLoading()">
                Approve Driver
              </button>
              <button class="btn btn-danger" (click)="showRejectDialog()" [disabled]="actionLoading()">
                Reject Application
              </button>
            }

            @if (driver()!.onboardingStatus === 'Approved') {
              <button class="btn btn-warning" (click)="showBanDialog()" [disabled]="actionLoading()">
                Suspend Driver
              </button>
            }

            @if (driver()!.onboardingStatus === 'Suspended') {
              <button class="btn btn-success" (click)="unsuspend()" [disabled]="actionLoading()">
                Unsuspend Driver
              </button>
            }
          </div>
        </div>

        @if (showRejectForm()) {
          <div class="modal-overlay" (click)="hideRejectDialog()">
            <div class="modal-content" (click)="$event.stopPropagation()">
              <h3>Reject Driver Application</h3>
              <div class="form-group">
                <label>Rejection Reason:</label>
                <textarea
                  class="form-control"
                  [(ngModel)]="rejectionReason"
                  rows="4"
                  placeholder="Provide a reason for rejection..."
                ></textarea>
              </div>
              <div class="modal-actions">
                <button class="btn btn-outline" (click)="hideRejectDialog()">Cancel</button>
                <button
                  class="btn btn-danger"
                  (click)="reject()"
                  [disabled]="!rejectionReason || actionLoading()"
                >
                  Confirm Rejection
                </button>
              </div>
            </div>
          </div>
        }

        @if (showBanForm()) {
          <div class="modal-overlay" (click)="hideBanDialog()">
            <div class="modal-content" (click)="$event.stopPropagation()">
              <h3>Suspend Driver</h3>
              <div class="form-group">
                <label>Suspension Reason:</label>
                <textarea
                  class="form-control"
                  [(ngModel)]="banReason"
                  rows="4"
                  placeholder="Provide a reason for suspension..."
                ></textarea>
              </div>
              <div class="modal-actions">
                <button class="btn btn-outline" (click)="hideBanDialog()">Cancel</button>
                <button
                  class="btn btn-warning"
                  (click)="ban()"
                  [disabled]="!banReason || actionLoading()"
                >
                  Confirm Suspension
                </button>
              </div>
            </div>
          </div>
        }

        @if (actionMessage()) {
          <div class="toast" [class.success]="actionSuccess()" [class.error]="!actionSuccess()">
            {{ actionMessage() }}
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .driver-details {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
    }

    h1 {
      margin: 0;
      color: #333;
    }

    h2 {
      color: #555;
      margin-bottom: 1rem;
      font-size: 1.3rem;
    }

    .content {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .section {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .info-item.full-width {
      grid-column: 1 / -1;
    }

    .info-item label {
      font-weight: 600;
      color: #666;
      font-size: 0.9rem;
    }

    .info-item span {
      color: #333;
    }

    .rejection-reason {
      padding: 0.75rem;
      background: #fff3cd;
      border-left: 4px solid #ffc107;
      border-radius: 4px;
    }

    .badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      border-radius: 12px;
      font-size: 0.85rem;
      font-weight: 500;
      width: fit-content;
    }

    .badge-pending {
      background: #fff3cd;
      color: #856404;
    }

    .badge-approved {
      background: #d4edda;
      color: #155724;
    }

    .badge-rejected {
      background: #f8d7da;
      color: #721c24;
    }

    .badge-suspended {
      background: #d6d8db;
      color: #383d41;
    }

    .documents-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .document-card {
      border: 1px solid #ddd;
      border-radius: 8px;
      overflow: hidden;
    }

    .document-card h3 {
      background: #f8f9fa;
      padding: 0.75rem;
      margin: 0;
      font-size: 1rem;
      color: #555;
    }

    .document-card img {
      width: 100%;
      height: 200px;
      object-fit: cover;
    }

    .document-info {
      padding: 0.75rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .document-info small {
      color: #666;
      font-size: 0.85rem;
    }

    .actions {
      display: flex;
      gap: 1rem;
      padding: 1.5rem;
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .loading {
      text-align: center;
      padding: 3rem;
      color: #666;
    }

    .btn {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 1rem;
      font-weight: 500;
      transition: all 0.2s;
    }

    .btn-outline {
      background: transparent;
      border: 1px solid #ddd;
      color: #666;
    }

    .btn-outline:hover {
      background: #f5f5f5;
    }

    .btn-success {
      background: #28a745;
      color: white;
    }

    .btn-success:hover:not(:disabled) {
      background: #218838;
    }

    .btn-danger {
      background: #dc3545;
      color: white;
    }

    .btn-danger:hover:not(:disabled) {
      background: #c82333;
    }

    .btn-warning {
      background: #ffc107;
      color: #333;
    }

    .btn-warning:hover:not(:disabled) {
      background: #e0a800;
    }

    .btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .modal-content {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      max-width: 500px;
      width: 90%;
    }

    .modal-content h3 {
      margin-top: 0;
      color: #333;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 600;
      color: #555;
    }

    .form-control {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 1rem;
      font-family: inherit;
    }

    .form-control:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .modal-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
    }

    .toast {
      position: fixed;
      bottom: 2rem;
      right: 2rem;
      padding: 1rem 1.5rem;
      border-radius: 4px;
      color: white;
      font-weight: 500;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
      animation: slideIn 0.3s ease-out;
    }

    .toast.success {
      background: #28a745;
    }

    .toast.error {
      background: #dc3545;
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }
  `]
})
export class DriverDetailsComponent implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  driver = signal<DriverDetails | null>(null);
  loading = signal(false);
  actionLoading = signal(false);
  showRejectForm = signal(false);
  showBanForm = signal(false);
  rejectionReason = '';
  banReason = '';
  actionMessage = signal<string | null>(null);
  actionSuccess = signal(false);

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    if (userId) {
      this.loadDriver(+userId);
    }
  }

  loadDriver(userId: number): void {
    this.loading.set(true);
    this.apiService.getDriverDetails(userId).subscribe({
      next: (driver) => {
        this.driver.set(driver);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.showMessage('Failed to load driver details', false);
      }
    });
  }

  approve(): void {
    if (!this.driver()) return;

    this.actionLoading.set(true);
    this.apiService.approveDriver(this.driver()!.userId).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.showMessage('Driver approved successfully', true);
        this.loadDriver(this.driver()!.userId);
      },
      error: () => {
        this.actionLoading.set(false);
        this.showMessage('Failed to approve driver', false);
      }
    });
  }

  reject(): void {
    if (!this.driver() || !this.rejectionReason) return;

    this.actionLoading.set(true);
    this.apiService.rejectDriver(this.driver()!.userId, this.rejectionReason).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.hideRejectDialog();
        this.showMessage('Driver application rejected', true);
        this.loadDriver(this.driver()!.userId);
      },
      error: () => {
        this.actionLoading.set(false);
        this.showMessage('Failed to reject driver', false);
      }
    });
  }

  ban(): void {
    if (!this.driver() || !this.banReason) return;

    this.actionLoading.set(true);
    this.apiService.banDriver(this.driver()!.userId, this.banReason).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.hideBanDialog();
        this.showMessage('Driver suspended', true);
        this.loadDriver(this.driver()!.userId);
      },
      error: () => {
        this.actionLoading.set(false);
        this.showMessage('Failed to suspend driver', false);
      }
    });
  }

  unsuspend(): void {
    if (!this.driver()) return;

    this.actionLoading.set(true);
    this.apiService.unbanDriver(this.driver()!.userId).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.showMessage('Driver unsuspended successfully', true);
        this.loadDriver(this.driver()!.userId);
      },
      error: () => {
        this.actionLoading.set(false);
        this.showMessage('Failed to unsuspend driver', false);
      }
    });
  }

  showRejectDialog(): void {
    this.rejectionReason = '';
    this.showRejectForm.set(true);
  }

  hideRejectDialog(): void {
    this.showRejectForm.set(false);
  }

  showBanDialog(): void {
    this.banReason = '';
    this.showBanForm.set(true);
  }

  hideBanDialog(): void {
    this.showBanForm.set(false);
  }

  showMessage(message: string, success: boolean): void {
    this.actionMessage.set(message);
    this.actionSuccess.set(success);
    setTimeout(() => this.actionMessage.set(null), 3000);
  }

  backToList(): void {
    this.router.navigate(['/drivers']);
  }

  getStatusClass(status: string): string {
    return status.toLowerCase();
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
