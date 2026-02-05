export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface DriverListItem {
  userId: number;
  fullName: string;
  phoneNumber: string;
  onboardingStatus: string;
  createdAt: string;
  approvedAt?: string;
}

export interface DriverDetails {
  userId: number;
  onboardingStatus: string;
  rejectionReason?: string;
  approvedAt?: string;
  createdAt: string;
  personalInfo?: PersonalInfo;
  vehicle?: VehicleInfo;
  documents: Document[];
}

export interface PersonalInfo {
  fullName: string;
  nationalId: string;
  dateOfBirth: string;
  address: string;
  city: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
}

export interface VehicleInfo {
  vehicleType: string;
  brand: string;
  model: string;
  color: string;
  licensePlate: string;
  year: number;
  vin: string;
}

export interface Document {
  documentType: string;
  imageUrl: string;
  uploadedAt: string;
  expiryDate?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
}
