import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard">
      <div class="header">
        <h1>Admin Dashboard</h1>
        <div class="user-info">
          <span>{{ authService.userName() }}</span>
          <button class="btn btn-sm btn-outline" (click)="logout()">Logout</button>
        </div>
      </div>

      <div class="stats-grid">
        <div class="stat-card">
          <h3>{{ stats().pending }}</h3>
          <p>Pending Applications</p>
          <button class="btn btn-link" (click)="navigateToDrivers('Pending')">View</button>
        </div>

        <div class="stat-card">
          <h3>{{ stats().approved }}</h3>
          <p>Approved Drivers</p>
          <button class="btn btn-link" (click)="navigateToDrivers('Approved')">View</button>
        </div>

        <div class="stat-card">
          <h3>{{ stats().rejected }}</h3>
          <p>Rejected Applications</p>
          <button class="btn btn-link" (click)="navigateToDrivers('Rejected')">View</button>
        </div>

        <div class="stat-card">
          <h3>{{ stats().suspended }}</h3>
          <p>Suspended Drivers</p>
          <button class="btn btn-link" (click)="navigateToDrivers('Suspended')">View</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      padding: 2rem;
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

    .user-info {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .stat-card {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      text-align: center;
    }

    .stat-card h3 {
      font-size: 3rem;
      margin: 0 0 0.5rem 0;
      color: #667eea;
    }

    .stat-card p {
      margin: 0 0 1rem 0;
      color: #666;
      font-size: 1.1rem;
    }

    .btn {
      padding: 0.5rem 1rem;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.9rem;
      transition: all 0.2s;
      text-decoration: none;
      border: none;
    }

    .btn-sm {
      padding: 0.4rem 0.8rem;
      font-size: 0.85rem;
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
      font-weight: 500;
    }

    .btn-link:hover {
      text-decoration: underline;
    }
  `]
})
export class DashboardComponent implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  stats = signal({
    pending: 0,
    approved: 0,
    rejected: 0,
    suspended: 0
  });

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    // Load each status count
    this.apiService.getDrivers(1, 1, 'Pending').subscribe({
      next: (result) => this.stats.update(s => ({ ...s, pending: result.totalCount }))
    });

    this.apiService.getDrivers(1, 1, 'Approved').subscribe({
      next: (result) => this.stats.update(s => ({ ...s, approved: result.totalCount }))
    });

    this.apiService.getDrivers(1, 1, 'Rejected').subscribe({
      next: (result) => this.stats.update(s => ({ ...s, rejected: result.totalCount }))
    });

    this.apiService.getDrivers(1, 1, 'Suspended').subscribe({
      next: (result) => this.stats.update(s => ({ ...s, suspended: result.totalCount }))
    });
  }

  navigateToDrivers(status: string): void {
    this.router.navigate(['/drivers'], { queryParams: { status } });
  }

  logout(): void {
    this.authService.logout();
  }
}
