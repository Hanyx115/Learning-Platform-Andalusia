import { useQuery } from '@tanstack/react-query'
import {
  getCareerPath,
  getCareerPaths,
  getCourse,
  getCourses,
  getProgram,
  getPrograms,
  type CatalogListQuery,
  type CourseCatalogQuery,
} from './catalog'

export const catalogQueryKeys = {
  courses: (query: CourseCatalogQuery) => ['courses', query] as const,
  course: (slug: string) => ['courses', slug] as const,
  programs: (query: CatalogListQuery) => ['programs', query] as const,
  program: (slug: string) => ['programs', slug] as const,
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
