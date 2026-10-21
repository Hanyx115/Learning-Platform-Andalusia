import { apiRequest } from './client'
import type { Profile } from './auth'
import type { Course, CourseSession, Offering, ProgramSession, ProgramSummary, TrainingLocation } from './catalog'

export interface CourseCategory {
  id: string
  name: string
  slug: string
}

export interface AdminEnrollment {
  id: string
  userId: string
  learnerName: string
  email: string
  applicationId: string
  paymentId: string
  courseOfferingId: string | null
  courseId: string | null
  courseTitle: string | null
  programOfferingId: string | null
  programId: string | null
  programTitle: string | null
  startsAtUtc: string
  endsAtUtc: string
  location: TrainingLocation | null
  status: string
  enrolledAt: string
  completedAt: string | null
  cancelledAt: string | null
}

export interface AdminPayment {
  id: string
  userId: string
  learnerName: string
  email: string
  applicationId: string
  courseTitle: string | null
  programTitle: string | null
  amount: number
  currency: string
  status: string
  createdAt: string
  completedAt: string | null
}

export interface ManageOfferingRequest {
  locationId?: string | null
  startsAtUtc: string
  endsAtUtc: string
  applicationOpensAtUtc: string
  applicationClosesAtUtc: string
  price: number
  currency: string
  capacity: number
  status: string
}

export interface ScheduleSessionRequest {
  locationId?: string | null
  instructorUserId: string
  title: string
  startsAtUtc: string
  endsAtUtc: string
}

export interface RescheduleSessionRequest extends ScheduleSessionRequest {
  note?: string | null
}

const jsonHeaders = { 'Content-Type': 'application/json' }

// Course categories
export function getCourseCategories() {
  return apiRequest<CourseCategory[]>('/api/v1/course-categories')
}

export function createCourseCategory(name: string) {
  return apiRequest<CourseCategory>('/api/v1/course-categories', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ name }),
  })
}

export function updateCourseCategory(id: string, name: string) {
  return apiRequest<CourseCategory>(`/api/v1/course-categories/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ name }),
  })
}

// Courses
export interface CreateCourseRequest {
  title: string
  categoryId: string
  description: string
  status: string
  type: string
  price: number | null
}

export function createCourse(request: CreateCourseRequest) {
  return apiRequest<Course>('/api/v1/courses', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateCourse(id: string, request: CreateCourseRequest) {
  return apiRequest<Course>(`/api/v1/courses/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

// Programs
export interface CreateProgramRequest {
  title: string
  description: string
  status: string
  price: number | null
}

export function createProgram(request: CreateProgramRequest) {
  return apiRequest<ProgramSummary>('/api/v1/programs', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateProgram(id: string, request: CreateProgramRequest) {
  return apiRequest<ProgramSummary>(`/api/v1/programs/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

// Training locations
export function getTrainingLocationsForManagement() {
  return apiRequest<TrainingLocation[]>('/api/v1/training-locations/manage')
}

export interface TrainingLocationRequest {
  branchName: string
  roomName: string
  city: string
  address: string
  mapUrl?: string | null
  capacity: number
  isActive?: boolean
}

export function createTrainingLocation(request: TrainingLocationRequest) {
  return apiRequest<TrainingLocation>('/api/v1/training-locations', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateTrainingLocation(id: string, request: TrainingLocationRequest) {
  return apiRequest<TrainingLocation>(`/api/v1/training-locations/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ ...request, isActive: request.isActive ?? true }),
  })
}

// Offerings
export function getCourseOfferingsForManagement(courseId: string) {
  return apiRequest<Offering[]>(`/api/v1/courses/${courseId}/offerings/manage`)
}

export function getProgramOfferingsForManagement(programId: string) {
  return apiRequest<Offering[]>(`/api/v1/programs/${programId}/offerings/manage`)
}

export function createCourseOffering(courseId: string, request: ManageOfferingRequest) {
  return apiRequest<Offering>(`/api/v1/courses/${courseId}/offerings`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateCourseOffering(offeringId: string, request: ManageOfferingRequest) {
  return apiRequest<Offering>(`/api/v1/course-offerings/${offeringId}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function createProgramOffering(programId: string, request: ManageOfferingRequest) {
  return apiRequest<Offering>(`/api/v1/programs/${programId}/offerings`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateProgramOffering(offeringId: string, request: ManageOfferingRequest) {
  return apiRequest<Offering>(`/api/v1/program-offerings/${offeringId}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

// Sessions
export function createCourseSession(courseOfferingId: string, request: ScheduleSessionRequest) {
  return apiRequest<CourseSession>(`/api/v1/course-offerings/${courseOfferingId}/sessions`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function rescheduleCourseSession(sessionId: string, request: RescheduleSessionRequest) {
  return apiRequest<CourseSession>(`/api/v1/course-sessions/${sessionId}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function cancelCourseSession(sessionId: string, reason: string) {
  return apiRequest<CourseSession>(`/api/v1/course-sessions/${sessionId}/cancel`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ reason }),
  })
}

export function createProgramSession(programOfferingId: string, request: ScheduleSessionRequest) {
  return apiRequest<ProgramSession>(`/api/v1/program-offerings/${programOfferingId}/sessions`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function rescheduleProgramSession(sessionId: string, request: RescheduleSessionRequest) {
  return apiRequest<ProgramSession>(`/api/v1/program-sessions/${sessionId}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function cancelProgramSession(sessionId: string, reason: string) {
  return apiRequest<ProgramSession>(`/api/v1/program-sessions/${sessionId}/cancel`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ reason }),
  })
}

// Directory
export function getAllEnrollments() {
  return apiRequest<AdminEnrollment[]>('/api/v1/enrollments')
}

export function getAllPayments() {
  return apiRequest<AdminPayment[]>('/api/v1/payments')
}

export function getUsersInRole(role: string) {
  return apiRequest<Profile[]>(`/api/v1/profile/by-role/${role}`)
}
