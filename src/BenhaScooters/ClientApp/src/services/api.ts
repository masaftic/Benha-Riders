import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Add token to requests
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('accessToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      try {
        const refreshToken = localStorage.getItem('refreshToken')
        const response = await axios.post('/api/auth/refresh', { refreshToken })
        const { accessToken, refreshToken: newRefreshToken } = response.data

        localStorage.setItem('accessToken', accessToken)
        localStorage.setItem('refreshToken', newRefreshToken)

        originalRequest.headers.Authorization = `Bearer ${accessToken}`
        return api(originalRequest)
      } catch (refreshError) {
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
        window.location.href = '/login'
        return Promise.reject(refreshError)
      }
    }

    return Promise.reject(error)
  }
)

interface LoginResponse {
  type: string;
  result: {
    accessToken: string
    refreshToken: string
  }
}

interface MeResponse {
  id: number
  name: string
  email: string
  phoneNumber: string
  roles: string[]
}


export type OnboardingStatus = 'Incomplete' | 'UnderReview' | 'Approved' | 'Rejected' | 'Suspended';

interface QueryDriversParams {
  onboardingStatus?: OnboardingStatus
  page?: number
  pageCount?: number
}

interface Driver {
  id: number
  fullName: string
  phoneNumber: string
  status: OnboardingStatus
  progress: number
}

interface DriversResponse {
  items: Driver[]
  page: number
  pageCount: number
  totalPages: number
  totalCount: number
}

/*     "onboardingState": {
        "status": "UnderReview",
        "progress": 75,
        "rejectionReason": null,
        "createdAt": "2026-02-09T19:55:11.526725Z",
        "completedAt": null,
        "isCompleted": false
    }, */

interface OnboardingState {
  status: OnboardingStatus
  progress: number
  rejectionReason?: string | null
  createdAt: string
  completedAt?: string | null
  isCompleted: boolean
}

interface DriverDetails {
  userId: number
  onboardingState: OnboardingState
  personalInfo?: {
    fullName: string
    nationalId: string
    phoneNumber: string
    email?: string
  }
  vehicleInfo?: {
    vehicleType: string
    brand: string
    model: string
    color: string
    licensePlate: string
    year: number
  }
  documents: DriverDocument[]
}

/* public enum DocumentType
{
    DrivingLicense,
    VehicleRegistration,
    DriverPhoto,
}
 */

export type DocumentType = 'DrivingLicense' | 'VehicleRegistration' | 'DriverPhoto';

interface DriverDocument {
  type: DocumentType
  imageUrl: string
  expirationDate?: string
}

interface DocumentsResponse {
  documents: DriverDocument[]
}

export const authAPI = {
  login: (phoneNumber: string, password: string) => 
    api.post<LoginResponse>('/auth/login', { phoneNumber, password }),
  
  logout: (refreshToken: string) => 
    api.post('/auth/logout', { refreshToken }),

  me: () => 
    api.get<MeResponse>('/auth/me'),
}

export const adminAPI = {
  getDrivers: (params: QueryDriversParams) => 
    api.get<DriversResponse>('/admin/drivers', { params }),

  getDriverDetails: (driverId: number) =>
    api.get<DriverDetails>(`/admin/drivers/${driverId}`),

  getDriverDocuments: (driverId: number) =>
    api.get<DocumentsResponse>(`/admin/drivers/${driverId}/documents`),

  approveDriver: (driverId: number) =>
    api.post(`/admin/drivers/${driverId}/approve`),

  banDriver: (driverId: number, reason: string) =>
    api.post(`/admin/drivers/${driverId}/ban`, { reason }),
}

export default api

export type { 
  LoginResponse, 
  MeResponse, 
  QueryDriversParams, 
  Driver, 
  DriversResponse, 
  DriverDetails, 
  DriverDocument, 
  DocumentsResponse 
}
