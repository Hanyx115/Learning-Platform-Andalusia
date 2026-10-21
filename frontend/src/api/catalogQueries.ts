import { useQuery } from '@tanstack/react-query'
import {
  getCareerPath,
  getCareerPaths,
  getCourse,
  getCourseOfferings,
  getCourseOfferingSessions,
  getCourses,
  getProgram,
  getProgramOfferings,
  getPrograms,
  type CatalogListQuery,
  type CourseCatalogQuery,
} from './catalog'

export const catalogQueryKeys = {
  courses: (query: CourseCatalogQuery) => ['courses', query] as const,
  course: (slug: string) => ['courses', slug] as const,
  courseOfferings: (slug: string) => ['courses', slug, 'offerings'] as const,
  courseOfferingSessions: (offeringId: string) => ['course-offerings', offeringId, 'sessions'] as const,
  programs: (query: CatalogListQuery) => ['programs', query] as const,
  program: (slug: string) => ['programs', slug] as const,
  programOfferings: (slug: string) => ['programs', slug, 'offerings'] as const,
  careerPaths: (query: CatalogListQuery) => ['career-paths', query] as const,
  careerPath: (slug: string) => ['career-paths', slug] as const,
}

export function useCourses(query: CourseCatalogQuery = {}) {
  return useQuery({
    queryKey: catalogQueryKeys.courses(query),
    queryFn: () => getCourses(query),
  })
}

export function useCourse(slug: string) {
  return useQuery({
    queryKey: catalogQueryKeys.course(slug),
    queryFn: () => getCourse(slug),
    enabled: Boolean(slug),
  })
}

export function useCourseOfferings(slug: string) {
  return useQuery({
    queryKey: catalogQueryKeys.courseOfferings(slug),
    queryFn: () => getCourseOfferings(slug),
    enabled: Boolean(slug),
  })
}

export function useCourseOfferingSessions(offeringId: string) {
  return useQuery({
    queryKey: catalogQueryKeys.courseOfferingSessions(offeringId),
    queryFn: () => getCourseOfferingSessions(offeringId),
    enabled: Boolean(offeringId),
  })
}

export function usePrograms(query: CatalogListQuery = {}) {
  return useQuery({
    queryKey: catalogQueryKeys.programs(query),
    queryFn: () => getPrograms(query),
  })
}

export function useProgram(slug: string) {
  return useQuery({
    queryKey: catalogQueryKeys.program(slug),
    queryFn: () => getProgram(slug),
    enabled: Boolean(slug),
  })
}

export function useProgramOfferings(slug: string) {
  return useQuery({
    queryKey: catalogQueryKeys.programOfferings(slug),
    queryFn: () => getProgramOfferings(slug),
    enabled: Boolean(slug),
  })
}

export function useCareerPaths(query: CatalogListQuery = {}) {
  return useQuery({
    queryKey: catalogQueryKeys.careerPaths(query),
    queryFn: () => getCareerPaths(query),
  })
}

export function useCareerPath(slug: string) {
  return useQuery({
    queryKey: catalogQueryKeys.careerPath(slug),
    queryFn: () => getCareerPath(slug),
    enabled: Boolean(slug),
  })
}
