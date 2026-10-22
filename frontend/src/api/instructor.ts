import { apiRequest } from './client'
import type { TrainingLocation } from './catalog'
import type { Assignment } from './content'

export type OfferingType = 'Course' | 'Program'

export type { Assignment }

export interface InstructorSession {
  sessionId: string
  courseOfferingId: string | null
  programOfferingId: string | null
  offeringType: OfferingType
  offeringTitle: string
  sessionTitle: string
  startsAtUtc: string
  endsAtUtc: string
  status: string
  location: TrainingLocation | null
}

export interface InstructorOffering {
  courseOfferingId: string | null
  programOfferingId: string | null
  courseId: string | null
  offeringType: OfferingType
  title: string
  startsAtUtc: string
  endsAtUtc: string
  status: string
  enrolledStudentCount: number
}

export interface InstructorStudent {
  userId: string
  learnerName: string
  email: string
  courseOfferingId: string | null
  programOfferingId: string | null
  offeringType: OfferingType
  offeringTitle: string
}

export interface SessionAttendanceRecord {
  userId: string
  learnerName: string
  email: string
  status: string | null
  note: string | null
  recordedByUserId: string | null
  recordedAtUtc: string | null
}

export interface AttendanceEntry {
  userId: string
  status: string
  note: string | null
}

export interface SubmissionRecord {
  userId: string
  learnerName: string
  email: string
  content: string | null
  submittedAtUtc: string | null
  score: number | null
  feedback: string | null
  gradedAtUtc: string | null
}

export interface OfferingProgressRecord {
  userId: string
  learnerName: string
  email: string
  totalSessions: number
  attendedSessions: number
  attendanceRatePercentage: number
  totalAssignments: number
  submittedAssignments: number
  gradedAssignments: number
  averageScorePercentage: number | null
  overallProgressPercentage: number
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function getMyInstructorSessions() {
  return apiRequest<InstructorSession[]>('/api/v1/instructor/sessions')
}

export function getMyInstructorOfferings() {
  return apiRequest<InstructorOffering[]>('/api/v1/instructor/offerings')
}

export function getMyInstructorStudents() {
  return apiRequest<InstructorStudent[]>('/api/v1/instructor/students')
}

export function getSessionAttendance(sessionId: string, offeringType: OfferingType) {
  const segment = offeringType === 'Course' ? 'course-sessions' : 'program-sessions'
  return apiRequest<SessionAttendanceRecord[]>(`/api/v1/${segment}/${sessionId}/attendance`)
}

export function markSessionAttendance(sessionId: string, offeringType: OfferingType, entries: AttendanceEntry[]) {
  const segment = offeringType === 'Course' ? 'course-sessions' : 'program-sessions'
  return apiRequest<SessionAttendanceRecord[]>(`/api/v1/${segment}/${sessionId}/attendance`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ entries }),
  })
}

export function getOfferingAssignments(offeringId: string, offeringType: OfferingType) {
  const segment = offeringType === 'Course' ? 'course-offerings' : 'program-offerings'
  return apiRequest<Assignment[]>(`/api/v1/${segment}/${offeringId}/assignments`)
}

export function getAssignmentSubmissions(assignmentId: string) {
  return apiRequest<SubmissionRecord[]>(`/api/v1/assignments/${assignmentId}/submissions`)
}

export function gradeSubmission(assignmentId: string, userId: string, score: number, feedback: string | null) {
  return apiRequest<SubmissionRecord>(`/api/v1/assignments/${assignmentId}/submissions/${userId}/grade`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify({ score, feedback }),
  })
}

export function getOfferingProgress(offeringId: string, offeringType: OfferingType) {
  const segment = offeringType === 'Course' ? 'course-offerings' : 'program-offerings'
  return apiRequest<OfferingProgressRecord[]>(`/api/v1/${segment}/${offeringId}/progress`)
}
