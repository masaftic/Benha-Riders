export type TopUpRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface TopUpRequest {
  id: number;
  driverUserId: number;
  driverName: string;
  amount: number;
  receiptUrl: string;
  status: TopUpRequestStatus;
  reviewNote?: string | null;
  createdAt: string;
  reviewedAt?: string | null;
}

export interface GetTopUpRequestsParams {
  page?: number;
  pageSize?: number;
  status?: TopUpRequestStatus;
  driverId?: number;
}

export interface ReviewTopUpRequestPayload {
  approve: boolean;
  note?: string;
}
