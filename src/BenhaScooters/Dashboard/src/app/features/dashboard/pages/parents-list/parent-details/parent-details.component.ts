import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ParentService } from '../../../../../core/services/parent.service';
import { ParentDetails as ParentDetailsModel } from '../../../../../core/models/parent.model';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DatePipe, DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-parent-details',
  imports: [
    ButtonModule,
    CardModule,
    ToastModule,
    DatePipe,
    DecimalPipe
  ],
  providers: [MessageService],
  templateUrl: './parent-details.component.html',
  styleUrl: './parent-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ParentDetails implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly parentService = inject(ParentService);
  private readonly messageService = inject(MessageService);

  parentDetails = signal<ParentDetailsModel | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.loadParentDetails(id);
    } else {
      this.router.navigate(['/dashboard/parents-list']);
    }
  }

  loadParentDetails(id: string) {
    this.loading.set(true);
    this.parentService.getById(id).subscribe({
      next: (parentDetails) => {
        this.parentDetails.set(parentDetails);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading parent details:', error);
        this.loading.set(false);
        this.router.navigate(['/dashboard/parents-list']);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard/parents-list']);
  }
}
