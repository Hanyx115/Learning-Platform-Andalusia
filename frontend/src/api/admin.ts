import { apiRequest } from './client'
import type { Profile } from './auth'
import type {
  CareerPathDetail,
  CareerPathSummary,
  Course,
  CourseSession,
  Offering,
  PaginatedResult,
  ProgramSession,
  ProgramSummary,
  TrainingLocation,
} from './catalog'

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
  statusHistory: PaymentStatusHistory[]
}

export interface PaymentStatusHistory {
  id: string
  status: string
  changedAt: string
  source: string
}

export interface AdminUser {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string
  isActive: boolean
  roles: string[]
}

export interface DirectoryUser {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string
  isActive: boolean
}

export interface DirectoryQuery {
  page?: number
  pageSize?: number
  search?: string
  status?: string
  type?: string
}

export interface StatusSummary {
  status: string
  count: number
}

export interface RevenueSummary {
  currency: string
  amount: number
  paymentCount: number
}

export interface OperationsActivity {
  id: string
  activityType: string
  status: string
  occurredAtUtc: string
  learnerName: string
  offeringTitle: string
}

export interface OperationsOverview {
  activeStudents: number
  activeInstructors: number
  runningCourses: number
  pendingApplications: number
  activeEnrollments: number
  revenue: RevenueSummary[]
  applicationsByStatus: StatusSummary[]
  paymentsByStatus: StatusSummary[]
  enrollmentsByStatus: StatusSummary[]
  recentActivity: OperationsActivity[]
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

function managementQuery(status: string, pageSize = 100) {
  return `?status=${encodeURIComponent(status)}&pageSize=${pageSize}`
}

function directoryQuery(query: DirectoryQuery) {
  const searchParams = new URLSearchParams()

  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      searchParams.set(key, String(value))
    }
  })

  const queryString = searchParams.toString()
  return queryString ? `?${queryString}` : ''
}

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

export type CourseContentRequest = Omit<CreateCourseRequest, 'price'>

export function getCoursesForManagement(status: string) {
  return apiRequest<PaginatedResult<Course>>(`/api/v1/courses/manage${managementQuery(status)}`)
}

export function createCourseContent(request: CourseContentRequest) {
  return apiRequest<Course>('/api/v1/courses/content', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateCourseContent(id: string, request: CourseContentRequest) {
  return apiRequest<Course>(`/api/v1/courses/${id}/content`, {
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

export type ProgramContentRequest = Omit<CreateProgramRequest, 'price'>

export function getProgramsForManagement(status: string) {
  return apiRequest<PaginatedResult<ProgramSummary>>(`/api/v1/programs/manage${managementQuery(status)}`)
}

export function createProgramContent(request: ProgramContentRequest) {
  return apiRequest<ProgramSummary>('/api/v1/programs/content', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateProgramContent(id: string, request: ProgramContentRequest) {
  return apiRequest<ProgramSummary>(`/api/v1/programs/${id}/content`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

// Career paths
export interface CareerPathRequest {
  title: string
  description: string
  recommendedSkills: string
  status: string
  recommendedProgramIds: string[]
  recommendedCourseIds: string[]
}

export function getCareerPathsForManagement(status: string) {
  return apiRequest<PaginatedResult<CareerPathSummary>>(`/api/v1/career-paths/manage${managementQuery(status)}`)
}

export function getCareerPathForManagement(id: string) {
  return apiRequest<CareerPathDetail>(`/api/v1/career-paths/manage/${id}`)
}

export function createCareerPath(request: CareerPathRequest) {
  return apiRequest<CareerPathDetail>('/api/v1/career-paths', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateCareerPath(id: string, request: CareerPathRequest) {
  return apiRequest<CareerPathDetail>(`/api/v1/career-paths/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function deleteCareerPath(id: string) {
  return apiRequest<void>(`/api/v1/career-paths/${id}`, { method: 'DELETE' })
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

export function getStudentDirectory(query: DirectoryQuery = {}) {
  return apiRequest<PaginatedResult<AdminEnrollment>>(`/api/v1/enrollments/manage${directoryQuery(query)}`)
}

export function getOperationsOverview() {
  return apiRequest<OperationsOverview>('/api/v1/operations/overview')
}

export function getAllPayments() {
  return apiRequest<AdminPayment[]>('/api/v1/payments')
}

export function failPendingPayment(paymentId: string) {
  return apiRequest<AdminPayment>(`/api/v1/payments/${paymentId}/fail`, {
    method: 'POST',
  })
}

export function getAdminUsers() {
  return apiRequest<AdminUser[]>('/api/v1/admin/users')
}

export function updateAdminUserStatus(userId: string, isActive: boolean) {
  return apiRequest<AdminUser>(`/api/v1/admin/users/${userId}/status`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ isActive }),
  })
}

export function updateAdminUserRoles(userId: string, roles: string[]) {
  return apiRequest<AdminUser>(`/api/v1/admin/users/${userId}/roles`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ roles }),
  })
}

export function getUsersInRole(role: string) {
  return apiRequest<Profile[]>(`/api/v1/profile/by-role/${role}`)
}

export function getInstructorDirectory(query: DirectoryQuery = {}) {
  return apiRequest<PaginatedResult<DirectoryUser>>(
    `/api/v1/profile/by-role/Instructor/manage${directoryQuery(query)}`,
  )
}
