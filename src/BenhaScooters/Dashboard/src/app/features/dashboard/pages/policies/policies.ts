import { Component, inject } from '@angular/core';
import { PolicyType } from '../../../../core/models/policy.model';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-policies',
  imports: [ButtonModule],
  templateUrl: './policies.html',
  styleUrl: './policies.scss',
})
export class PoliciesComponent {
  policies = [
    {
      type: PolicyType.PrivacyPolicy,
      title: 'Privacy Policy',
    },
    {
      type: PolicyType.TermsOfService,
      title: 'Terms of Service',
    },
  ];

  router = inject(Router);

  edit(type: PolicyType) {
    this.router.navigate(['/dashboard/policies', type]);
  }
}
