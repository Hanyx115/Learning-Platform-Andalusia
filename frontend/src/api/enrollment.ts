import { apiRequest } from './client'

export interface Application {
  id: string
  userId: string
  courseId: string | null
  courseTitle: string | null
  programId: string | null
  programTitle: string | null
  status: string
  submittedAt: string
  reviewedAt: string | null
  rejectionReason: string | null
}

export interface ApplicationListQuery {
  page?: number
  pageSize?: number
  status?: string
}

export interface PaginatedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface Payment {
  id: string
  applicationId: string
  amount: number
  currency: string
  status: string
  createdAt: string
  completedAt: string | null
}

export interface Enrollment {
  id: string
  applicationId: string
  paymentId: string
  courseId: string | null
  courseTitle: string | null
  programId: string | null
  programTitle: string | null
  status: string
  enrolledAt: string
  completedAt: string | null
  cancelledAt: string | null
}

export interface PayForApplicationResult {
  succeeded: boolean
  payment: Payment | null
  enrollment: Enrollment | null
  error: string | null
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function submitApplication(target: { courseId?: string; programId?: string }) {
  return apiRequest<Application>('/api/v1/applications', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(target),
  })
}

export function getMyApplications() {
  return apiRequest<Application[]>('/api/v1/applications/me')
}

function buildQueryString(query: ApplicationListQuery) {
  const searchParams = new URLSearchParams()

  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      searchParams.set(key, String(value))
    }
  })

  const queryString = searchParams.toString()
  return queryString ? `?${queryString}` : ''
}

export function getApplicationsPage(query: ApplicationListQuery = {}) {
  return apiRequest<PaginatedResult<Application>>(`/api/v1/applications${buildQueryString(query)}`)
}

export function acceptApplication(applicationId: string) {
  return apiRequest<Application>(`/api/v1/applications/${applicationId}/accept`, {
    method: 'POST',
  })
}

export function rejectApplication(applicationId: string, reason: string) {
  return apiRequest<Application>(`/api/v1/applications/${applicationId}/reject`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ reason }),
  })
}

export function payForApplication(applicationId: string) {
  return apiRequest<PayForApplicationResult>('/api/v1/payments', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ applicationId }),
  })
}

export function getMyPayments() {
  return apiRequest<Payment[]>('/api/v1/payments/me')
}

export function getMyEnrollments() {
  return apiRequest<Enrollment[]>('/api/v1/enrollments/me')
}
