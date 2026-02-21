export interface PaginationRequest {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  orderBy?: string;
  isDescending?: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
