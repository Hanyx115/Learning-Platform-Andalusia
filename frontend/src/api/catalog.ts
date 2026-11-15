import { apiRequest } from './client'

export interface Course {
  id: string
  title: string
  slug: string
  description: string
  status: string
  type: string
  price: number | null
  imageUrl?: string | null
  categorySlug: string
  categoryName: string
  createdAt: string
}

export interface CourseCatalogQuery {
  page?: number
  pageSize?: number
  search?: string
  category?: string
  status?: string
  type?: string
  sort?: 'title' | 'title-desc' | 'newest' | 'oldest'
}

export interface CatalogListQuery {
  page?: number
  pageSize?: number
  status?: string
  search?: string
  sort?: 'title' | 'title-desc'
}

export interface PaginatedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ProgramSummary {
  id: string
  title: string
  slug: string
  description: string
  status: string
  price: number | null
  imageUrl?: string | null
  createdAt: string
}

export interface ProgramCourse {
  courseId: string
  courseTitle: string
  courseSlug: string
  sequenceOrder: number
}

export interface ProgramDetail extends ProgramSummary {
  courses: ProgramCourse[]
}

export interface Offering {
  id: string
  startsAtUtc: string
  endsAtUtc: string
  applicationOpensAtUtc: string
  applicationClosesAtUtc: string
  price: number
  currency: string
  capacity: number
  status: string
  location: TrainingLocation | null
}

export interface TrainingLocation {
  id: string
  branchName: string
  roomName: string
  city: string
  address: string
  mapUrl: string | null
  capacity: number
  isActive: boolean
}

export interface CourseSession {
  id: string
  courseOfferingId: string
  courseTitle: string
  title: string
  startsAtUtc: string
  endsAtUtc: string
  status: string
  cancellationReason: string | null
  reschedulingNote: string | null
  instructorUserId: string | null
  instructorName: string | null
  location: TrainingLocation | null
}

export interface ProgramSession {
  id: string
  programOfferingId: string
  programTitle: string
  title: string
  startsAtUtc: string
  endsAtUtc: string
  status: string
  cancellationReason: string | null
  reschedulingNote: string | null
  instructorUserId: string | null
  instructorName: string | null
  location: TrainingLocation | null
}

export interface CareerPathSummary {
  id: string
  title: string
  slug: string
  description: string
  status: string
  imageUrl?: string | null
}

export interface CareerPathProgram {
  programId: string
  programTitle: string
  programSlug: string
}

export interface CareerPathCourse {
  courseId: string
  courseTitle: string
  courseSlug: string
}

export interface CareerPathDetail extends CareerPathSummary {
  recommendedSkills: string
  recommendedPrograms: CareerPathProgram[]
  recommendedCourses: CareerPathCourse[]
}

function buildQueryString(query: CourseCatalogQuery | CatalogListQuery) {
  const searchParams = new URLSearchParams()

  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== '') {
      searchParams.set(key, String(value))
    }
  })

  const queryString = searchParams.toString()
  return queryString ? `?${queryString}` : ''
}

export function getCourses(query: CourseCatalogQuery = {}) {
  return apiRequest<PaginatedResult<Course>>(`/api/v1/courses${buildQueryString(query)}`)
}

export function getCourse(slug: string) {
  return apiRequest<Course>(`/api/v1/courses/${encodeURIComponent(slug)}`)
}

export function getCourseOfferings(slug: string) {
  return apiRequest<Offering[]>(`/api/v1/courses/${encodeURIComponent(slug)}/offerings`)
}

export function getCourseOfferingSessions(offeringId: string) {
  return apiRequest<CourseSession[]>(
    `/api/v1/course-offerings/${encodeURIComponent(offeringId)}/sessions`,
  )
}

export function getPrograms(query: CatalogListQuery = {}) {
  return apiRequest<PaginatedResult<ProgramSummary>>(`/api/v1/programs${buildQueryString(query)}`)
}

export function getProgram(slug: string) {
  return apiRequest<ProgramDetail>(`/api/v1/programs/${encodeURIComponent(slug)}`)
}

export function getProgramOfferings(slug: string) {
  return apiRequest<Offering[]>(`/api/v1/programs/${encodeURIComponent(slug)}/offerings`)
}

export function getProgramOfferingSessions(offeringId: string) {
  return apiRequest<ProgramSession[]>(
    `/api/v1/program-offerings/${encodeURIComponent(offeringId)}/sessions`,
  )
}

export function getCareerPaths(query: CatalogListQuery = {}) {
  return apiRequest<PaginatedResult<CareerPathSummary>>(`/api/v1/career-paths${buildQueryString(query)}`)
}

export function getCareerPath(slug: string) {
  return apiRequest<CareerPathDetail>(`/api/v1/career-paths/${encodeURIComponent(slug)}`)
}
