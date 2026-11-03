import { apiDownload, apiRequest } from './client'
import type { TrainingLocation } from './catalog'

export interface Application {
  id: string
  userId: string
  courseOfferingId: string | null
  courseId: string | null
  courseTitle: string | null
  programOfferingId: string | null
  programId: string | null
  programTitle: string | null
  startsAtUtc: string
  endsAtUtc: string
  price: number
  currency: string
  status: string
  submittedAt: string
  reviewedAt: string | null
  rejectionReason: string | null
  statusHistory: ApplicationStatusHistory[]
}

export interface UpcomingSession {
  enrollmentId: string
  sessionId: string
  courseOfferingId: string | null
  programOfferingId: string | null
  offeringType: 'Course' | 'Program'
  offeringTitle: string
  sessionTitle: string
  startsAtUtc: string
  endsAtUtc: string
  status: string
  cancellationReason: string | null
  instructorName: string | null
  location: TrainingLocation | null
}

export interface ApplicationStatusHistory {
  id: string
  status: string
  changedAt: string
  note: string | null
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
  statusHistory?: PaymentStatusHistory[]
}

export interface PaymentStatusHistory {
  id: string
  status: string
  changedAt: string
  source: string
}

export interface Enrollment {
  id: string
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

export interface CourseMaterial {
  id: string
  courseId: string
  courseSessionId: string | null
  sessionTitle: string | null
  title: string
  description: string | null
  resourceUrl: string
  materialType: string
  sortOrder: number
  visibility: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface PayForApplicationResult {
  succeeded: boolean
  payment: Payment | null
  enrollment: Enrollment | null
  error: string | null
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function submitApplication(target: { courseOfferingId?: string; programOfferingId?: string }) {
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

export function getMyUpcomingSessions() {
  return apiRequest<UpcomingSession[]>('/api/v1/learner/upcoming-sessions')
}

export interface LearnerAttendance {
  sessionId: string
  sessionTitle: string
  courseTitle: string | null
  programTitle: string | null
  startsAtUtc: string
  status: string
  note: string | null
}

export function getMyAttendance() {
  return apiRequest<LearnerAttendance[]>('/api/v1/learner/attendance')
}

export function getEnrollmentMaterials(enrollmentId: string) {
  return apiRequest<CourseMaterial[]>(`/api/v1/learner/enrollments/${enrollmentId}/materials`)
}

export interface CourseAssistantHistoryMessage {
  role: 'user' | 'assistant'
  content: string
}

export interface CourseAssistantCitation {
  materialId: string
  title: string
  resourceUrl: string
  materialType: string
}

export interface CourseAssistantResponse {
  answer: string
  citations: CourseAssistantCitation[]
  isDemoMode: boolean
}

export function askCourseAssistant(
  enrollmentId: string,
  question: string,
  language: string,
  history: CourseAssistantHistoryMessage[],
) {
  return apiRequest<CourseAssistantResponse>(
    `/api/v1/learner/enrollments/${enrollmentId}/assistant/messages`,
    {
      method: 'POST',
      headers: jsonHeaders,
      body: JSON.stringify({ question, language, history }),
    },
  )
}

export interface LearnerAssignment {
  assignmentId: string
  title: string
  description: string | null
  courseOfferingId: string | null
  courseTitle: string | null
  programOfferingId: string | null
  programTitle: string | null
  dueAtUtc: string
  maxScore: number
  mySubmissionContent: string | null
  mySubmissionType: 'Text' | 'Link' | 'File' | null
  mySubmissionLinkUrl: string | null
  mySubmissionFileName: string | null
  mySubmissionFileSizeBytes: number | null
  mySubmittedAtUtc: string | null
  myScore: number | null
  myFeedback: string | null
}

export function getMyAssignments() {
  return apiRequest<LearnerAssignment[]>('/api/v1/learner/assignments')
}

export function submitAssignment(assignmentId: string, content: string) {
  return apiRequest<{ assignmentId: string; content: string; submittedAtUtc: string }>(
    `/api/v1/assignments/${assignmentId}/submit`,
    {
      method: 'POST',
      headers: jsonHeaders,
      body: JSON.stringify({ content }),
    },
  )
}

export function submitAssignmentLink(assignmentId: string, url: string) {
  return apiRequest<{
    assignmentId: string
    content: string
    submissionType: string
    linkUrl: string | null
    originalFileName: string | null
    fileSizeBytes: number | null
    submittedAtUtc: string
  }>(`/api/v1/assignments/${assignmentId}/submit-link`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ url }),
  })
}

export function submitAssignmentFile(assignmentId: string, file: File) {
  const formData = new FormData()
  formData.append('file', file)

  return apiRequest<{
    assignmentId: string
    content: string
    submissionType: string
    linkUrl: string | null
    originalFileName: string | null
    fileSizeBytes: number | null
    submittedAtUtc: string
  }>(`/api/v1/assignments/${assignmentId}/submit-file`, {
    method: 'POST',
    body: formData,
  })
}

export function downloadMyAssignmentFile(assignmentId: string) {
  return apiDownload(`/api/v1/assignments/${assignmentId}/submission/file`)
}

export interface LearnerProgress {
  enrollmentId: string
  courseOfferingId: string | null
  courseTitle: string | null
  programOfferingId: string | null
  programTitle: string | null
  totalSessions: number
  attendedSessions: number
  attendanceRatePercentage: number
  totalAssignments: number
  submittedAssignments: number
  gradedAssignments: number
  averageScorePercentage: number | null
  overallProgressPercentage: number
}

export function getMyProgress() {
  return apiRequest<LearnerProgress[]>('/api/v1/learner/progress')
}
