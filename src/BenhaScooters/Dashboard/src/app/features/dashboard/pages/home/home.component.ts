import { Component, ChangeDetectionStrategy, OnInit, inject, signal } from '@angular/core';
import { CardModule } from 'primeng/card';
import { CommonModule } from '@angular/common';
import { DashboardStatsService } from '../../../../core/services/dashboard-stats.service';
import { AdminDashboardStatsResponse } from '../../../../core/models/dashboard-stats.model';

@Component({
  selector: 'app-home',
  imports: [CardModule, CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private dashboardStatsService = inject(DashboardStatsService);

  stats = signal<AdminDashboardStatsResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadStats();
  }

  loadStats(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardStatsService.getDashboardStats().subscribe({
      next: (data) => {
        this.stats.set(data);
        this.loading.set(false);
      }
    });
  }
}
