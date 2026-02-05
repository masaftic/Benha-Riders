import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { DriverListItem, PagedResult } from '../../models/driver.models';

@Component({
  selector: 'app-drivers-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="drivers-page">
      <div class="header">
        <h1>Drivers</h1>
        <button class="btn btn-outline" (click)="backToDashboard()">
          ← Back to Dashboard
        </button>
      </div>

      <div class="filters">
        <div class="filter-group">
          <label>Status:</label>
          <select class="form-control" [(ngModel)]="selectedStatus" (change)="loadDrivers()">
            <option value="">All</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
            <option value="Suspended">Suspended</option>
          </select>
        </div>

        <div class="filter-group">
          <label>Search:</label>
          <input
            type="text"
            class="form-control"
            [(ngModel)]="searchTerm"
            (keyup.enter)="loadDrivers()"
            placeholder="Search by name or phone..."
          />
        </div>

        <button class="btn btn-primary" (click)="loadDrivers()">Search</button>
      </div>

      @if (loading()) {
        <div class="loading">Loading drivers...</div>
      } @else {
        <div class="table-container">
          <table class="drivers-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Full Name</th>
                <th>Phone</th>
                <th>Status</th>
                <th>Created At</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (driver of drivers().items; track driver.userId) {
                <tr>
                  <td>{{ driver.userId }}</td>
                  <td>{{ driver.fullName }}</td>
                  <td>{{ driver.phoneNumber }}</td>
                  <td>
                    <span [class]="'badge badge-' + getStatusClass(driver.onboardingStatus)">
                      {{ driver.onboardingStatus }}
                    </span>
                  </td>
                  <td>{{ formatDate(driver.createdAt) }}</td>
                  <td>
                    <button
                      class="btn btn-sm btn-link"
                      (click)="viewDriver(driver.userId)"
                    >
                      View Details
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="6" class="text-center">No drivers found</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (drivers().totalPages > 1) {
          <div class="pagination">
            <button
              class="btn btn-sm"
              [disabled]="currentPage() === 1"
              (click)="previousPage()"
            >
              Previous
            </button>
            <span class="page-info">
              Page {{ currentPage() }} of {{ drivers().totalPages }}
              ({{ drivers().totalCount }} total)
            </span>
            <button
              class="btn btn-sm"
              [disabled]="currentPage() === drivers().totalPages"
              (click)="nextPage()"
            >
              Next
            </button>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .drivers-page {
      padding: 2rem;
      max-width: 1400px;
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

    .filters {
      display: flex;
      gap: 1rem;
      margin-bottom: 2rem;
      flex-wrap: wrap;
    }

    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      flex: 1;
      min-width: 200px;
    }

    .filter-group label {
      font-weight: 500;
      color: #555;
    }

    .form-control {
      padding: 0.5rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 0.9rem;
    }

    .form-control:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .table-container {
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      overflow: hidden;
      margin-bottom: 1rem;
    }

    .drivers-table {
      width: 100%;
      border-collapse: collapse;
    }

    .drivers-table th {
      background: #f8f9fa;
      padding: 1rem;
      text-align: left;
      font-weight: 600;
      color: #333;
      border-bottom: 2px solid #dee2e6;
    }

    .drivers-table td {
      padding: 1rem;
      border-bottom: 1px solid #dee2e6;
    }

    .drivers-table tbody tr:hover {
      background: #f8f9fa;
    }

    .badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      border-radius: 12px;
      font-size: 0.85rem;
      font-weight: 500;
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

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
    }

    .page-info {
      color: #666;
    }

    .loading {
      text-align: center;
      padding: 3rem;
      color: #666;
    }

    .text-center {
      text-align: center;
    }

    .btn {
      padding: 0.5rem 1rem;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.9rem;
      transition: all 0.2s;
    }

    .btn-sm {
      padding: 0.4rem 0.8rem;
      font-size: 0.85rem;
    }

    .btn-primary {
      background: #667eea;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #5568d3;
    }

    .btn-outline {
      background: transparent;
      border: 1px solid #ddd;
      color: #666;
    }

    .btn-outline:hover {
      background: #f5f5f5;
    }

    .btn-link {
      background: transparent;
      color: #667eea;
      padding: 0.25rem 0.5rem;
    }

    .btn-link:hover {
      text-decoration: underline;
    }

    .btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  `]
})
export class DriversListComponent implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  drivers = signal<PagedResult<DriverListItem>>({
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 0
  });

  loading = signal(false);
  currentPage = signal(1);
  selectedStatus = '';
  searchTerm = '';

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.selectedStatus = params['status'] || '';
      this.loadDrivers();
    });
  }

  loadDrivers(): void {
    this.loading.set(true);
    this.apiService.getDrivers(
      this.currentPage(),
      20,
      this.selectedStatus || undefined,
      this.searchTerm || undefined
    ).subscribe({
      next: (result) => {
        this.drivers.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  viewDriver(userId: number): void {
    this.router.navigate(['/drivers', userId]);
  }

  backToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadDrivers();
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.drivers().totalPages) {
      this.currentPage.update(p => p + 1);
      this.loadDrivers();
    }
  }

  getStatusClass(status: string): string {
    return status.toLowerCase();
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
