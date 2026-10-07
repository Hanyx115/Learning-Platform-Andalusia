import { apiRequest } from './client'

export interface Course {
  id: string
  title: string
  slug: string
  description: string
  status: string
  type: string
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

export interface CareerPathSummary {
  id: string
  title: string
  slug: string
  description: string
  status: string
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

export function getPrograms(query: CatalogListQuery = {}) {
  return apiRequest<PaginatedResult<ProgramSummary>>(`/api/v1/programs${buildQueryString(query)}`)
}

export function getProgram(slug: string) {
  return apiRequest<ProgramDetail>(`/api/v1/programs/${encodeURIComponent(slug)}`)
}

export function getCareerPaths(query: CatalogListQuery = {}) {
  return apiRequest<PaginatedResult<CareerPathSummary>>(`/api/v1/career-paths${buildQueryString(query)}`)
}

export function getCareerPath(slug: string) {
  return apiRequest<CareerPathDetail>(`/api/v1/career-paths/${encodeURIComponent(slug)}`)
}
