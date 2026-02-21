
// policy-type.ts
export enum PolicyType {
  PrivacyPolicy = 'PrivacyPolicy',
  TermsOfService = 'TermsOfService',
}

export interface PolicyResponse {
  content: string;
  type: PolicyType;
  createdAt: string; // ISO date string
  updatedAt: string; // ISO date string
}


export interface UpdatePolicyRequest {
  content: string;
}
