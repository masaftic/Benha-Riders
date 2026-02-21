import { Component, inject, model, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PolicyService } from '../../../../../core/services/policy.service';
import { MessageService } from 'primeng/api';
import { PolicyType } from '../../../../../core/models/policy.model';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from "primeng/toast";
import { EditorModule } from 'primeng/editor';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-policy-editor',
  imports: [ButtonModule, ToastModule, EditorModule, FormsModule, DatePipe],
  providers: [MessageService],
  templateUrl: './policy-editor.html',
  styleUrl: './policy-editor.scss',
})
export class PolicyEditor {
  activatedRoute = inject(ActivatedRoute);
  policyService = inject(PolicyService);
  messageService = inject(MessageService);
  router = inject(Router);

  policyType: PolicyType | null = null;
  policyTitle = '';
  policyContent = model<string>('');
  lastUpdated = signal<string | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.policyType = this.activatedRoute.snapshot.params['type'] as PolicyType;
    this.policyTitle = this.policyType === 'PrivacyPolicy' ? 'Privacy Policy' : 'Terms of Service';

    this.loadPolicy();
  }

  loadPolicy() {
    if (!this.policyType) return;

    this.loading.set(true);

    this.policyService.getByType(this.policyType).subscribe(response => {
      this.policyContent.set(response.content);
      this.lastUpdated.set(response.updatedAt ?? response.createdAt ?? null);
      this.loading.set(false);
    });
  }

  savePolicy() {
    if (!this.policyType) return;

    this.loading.set(true);
    this.policyService.upsert(this.policyType, this.policyContent())
      .subscribe({
        next: () => {
          this.lastUpdated.set(new Date().toISOString());
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Policy saved successfully.' });
          this.error.set(null);
          this.loading.set(false);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to save policy.' });
          this.error.set('Failed to save policy.');
          this.loading.set(false);
        }
      });
  }

  goBack() {
    this.router.navigate(['/dashboard/policies']);
  }

  getDaysAgoMessage(dateString: string | null): string | null {
    if (!dateString) return null;
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    if (diffTime < 1000 * 60) {
      return 'Just now';
    } else if (diffTime < 1000 * 60 * 60) {
      return `${Math.ceil(diffTime / (1000 * 60))} minutes ago`;
    } else if (diffTime < 1000 * 60 * 60 * 24) {
      return `${Math.ceil(diffTime / (1000 * 60 * 60))} hours ago`;
    } else {
      return `${Math.ceil(diffTime / (1000 * 60 * 60 * 24))} days ago`;
    }
  }
}
