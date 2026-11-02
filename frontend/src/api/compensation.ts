import { apiRequest } from './client'

export interface Balance {
  balance: number
  currency: string
  updatedAtUtc: string | null
}

export interface InstructorCompensationTarget {
  rateId: string | null
  instructorUserId: string
  instructorName: string
  courseOfferingId: string
  courseTitle: string
  startsAtUtc: string
  endsAtUtc: string
  offeringStatus: string
  amount: number | null
  currency: string
  isEligibleForPayout: boolean
  payoutStatus: string | null
}

export interface StaffCompensationRate {
  userId: string
  staffName: string
  role: string
  monthlyAmount: number | null
  currency: string
}

export interface CompensationPayout {
  id: string
  userId: string
  payeeName: string
  payoutType: string
  description: string
  amount: number
  currency: string
  status: string
  createdAtUtc: string
  reviewedAtUtc: string | null
  approvedAtUtc: string | null
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function getMyBalance() {
  return apiRequest<Balance>('/api/v1/balance/me')
}

export function addDemoBalanceCredit(amount: number, method: string) {
  return apiRequest<Balance>('/api/v1/balance/me/demo-credit', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ amount, method }),
  })
}

export function getInstructorCompensationTargets() {
  return apiRequest<InstructorCompensationTarget[]>('/api/v1/compensation/instructor-targets')
}

export function setInstructorCourseRate(target: InstructorCompensationTarget, amount: number) {
  return apiRequest<void>('/api/v1/compensation/instructor-rates', {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({
      instructorUserId: target.instructorUserId,
      courseOfferingId: target.courseOfferingId,
      amount,
      currency: 'EGP',
    }),
  })
}

export function getStaffCompensationRates() {
  return apiRequest<StaffCompensationRate[]>('/api/v1/compensation/staff-rates')
}

export function setStaffMonthlyRate(userId: string, amount: number) {
  return apiRequest<void>(`/api/v1/compensation/staff-rates/${userId}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ amount, currency: 'EGP' }),
  })
}

export function getCompensationPayouts() {
  return apiRequest<CompensationPayout[]>('/api/v1/compensation/payouts')
}

export function createInstructorPayout(rateId: string) {
  return apiRequest<CompensationPayout>(`/api/v1/compensation/instructor-rates/${rateId}/payout`, { method: 'POST' })
}

export function createStaffPayout(userId: string, year: number, month: number) {
  return apiRequest<CompensationPayout>(`/api/v1/compensation/staff-rates/${userId}/payout`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ year, month }),
  })
}

export function reviewCompensationPayout(payoutId: string) {
  return apiRequest<CompensationPayout>(`/api/v1/compensation/payouts/${payoutId}/review`, { method: 'POST' })
}

export function approveCompensationPayout(payoutId: string) {
  return apiRequest<CompensationPayout>(`/api/v1/compensation/payouts/${payoutId}/approve`, { method: 'POST' })
}
