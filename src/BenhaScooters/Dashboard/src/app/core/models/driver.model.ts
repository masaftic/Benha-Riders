export type OnboardingStatus = 'Incomplete' | 'UnderReview' | 'Approved' | 'Rejected' | 'Suspended';

export type DocumentType = 'DrivingLicense' | 'VehicleRegistration' | 'DriverPhoto';

export interface DriverSummary {
  id: number;
  fullName: string;
  phoneNumber: string;
  status: OnboardingStatus;
  progress: number;
}

export interface OnboardingState {
  status: OnboardingStatus;
  progress: number;
  rejectionReason?: string | null;
  createdAt: string;
  completedAt?: string | null;
  isCompleted: boolean;
}

export interface PersonalInfo {
  fullName: string;
  nationalId: string;
  phoneNumber: string;
  email?: string;
}

export interface VehicleInfo {
  vehicleType: string;
  brand: string;
  model: string;
  color: string;
  licensePlate: string;
  year: number;
}

export interface DriverDocument {
  type: DocumentType;
  imageUrl: string;
  expirationDate?: string;
}

export interface DriverDetails {
  userId: number;
  onboardingState: OnboardingState;
  personalInfo?: PersonalInfo;
  vehicleInfo?: VehicleInfo;
  documents: DriverDocument[];
}
