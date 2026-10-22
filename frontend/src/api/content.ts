import { apiRequest } from './client'
import type { OfferingType } from './instructor'

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

export interface CourseMaterialRequest {
  courseSessionId?: string | null
  title: string
  description?: string | null
  resourceUrl: string
  materialType: string
  sortOrder: number
  visibility: string
}

export interface Assignment {
  id: string
  courseOfferingId: string | null
  programOfferingId: string | null
  title: string
  description: string | null
  dueAtUtc: string
  maxScore: number
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface AssignmentRequest {
  title: string
  description?: string | null
  dueAtUtc: string
  maxScore: number
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function getCourseMaterialsForManagement(courseId: string) {
  return apiRequest<CourseMaterial[]>(`/api/v1/courses/${courseId}/materials`)
}

export function createCourseMaterial(courseId: string, request: CourseMaterialRequest) {
  return apiRequest<CourseMaterial>(`/api/v1/courses/${courseId}/materials`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateCourseMaterial(id: string, request: CourseMaterialRequest) {
  return apiRequest<CourseMaterial>(`/api/v1/course-materials/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function createOfferingAssignment(offeringId: string, offeringType: OfferingType, request: AssignmentRequest) {
  const segment = offeringType === 'Course' ? 'course-offerings' : 'program-offerings'
  return apiRequest<Assignment>(`/api/v1/${segment}/${offeringId}/assignments`, {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateAssignment(id: string, request: AssignmentRequest) {
  return apiRequest<Assignment>(`/api/v1/assignments/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}
