import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, vi } from 'vitest'
import CareerPathDetailPage from './CareerPathDetailPage'
import CareerPathsPage from './CareerPathsPage'
import CoursesPage from './CoursesPage'
import ProgramDetailPage from './ProgramDetailPage'
import ProgramsPage from './ProgramsPage'

function renderWithCatalogProviders(ui: React.ReactNode, initialEntry: string) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
        {ui}
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

function jsonResponse(body: unknown) {
  return Promise.resolve(new Response(JSON.stringify(body), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  }))
}

describe('catalog pages', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('renders courses returned by the paginated API', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse({
      items: [{
        id: 'course-1',
        title: 'Programming Fundamentals',
        slug: 'programming-fundamentals',
        description: 'Learn programming concepts.',
        status: 'Published',
        type: 'Technical',
        categoryName: 'Computer Engineering',
        createdAt: '2026-01-01T00:00:00Z',
      }],
      page: 1,
      pageSize: 6,
      totalCount: 1,
      totalPages: 1,
    })))

    renderWithCatalogProviders(<CoursesPage />, '/courses')

    expect(await screen.findByRole('heading', { name: 'Programming Fundamentals' }, { timeout: 5_000 })).toBeInTheDocument()
    expect(screen.getAllByText('Computer Engineering')).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'View details' })).toHaveAttribute(
      'href',
      '/courses/programming-fundamentals',
    )
  })

  it('renders the ordered courses included in a program', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse({
      id: 'program-1',
      title: 'Software Engineering Foundations',
      slug: 'software-engineering-foundations',
      description: 'A structured program.',
      status: 'Published',
      createdAt: '2026-01-01T00:00:00Z',
      courses: [{
        courseId: 'course-1',
        courseTitle: 'Programming Fundamentals',
        courseSlug: 'programming-fundamentals',
        sequenceOrder: 1,
      }],
    })))

    renderWithCatalogProviders(
      <Routes>
        <Route path="/programs/:slug" element={<ProgramDetailPage />} />
      </Routes>,
      '/programs/software-engineering-foundations',
    )

    expect(await screen.findByRole('heading', { name: 'Software Engineering Foundations' }, { timeout: 5_000 })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Programming Fundamentals' })).toHaveAttribute(
      'href',
      '/courses/programming-fundamentals',
    )
  })

  it('renders paginated programs and requests the next page', async () => {
    const fetchMock = vi.fn(() => jsonResponse({
      items: [{
        id: 'program-1',
        title: 'Business Management Essentials',
        slug: 'business-management-essentials',
        description: 'A structured business program.',
        status: 'Published',
        createdAt: '2026-01-01T00:00:00Z',
      }],
      page: 1,
      pageSize: 6,
      totalCount: 9,
      totalPages: 2,
    }))
    vi.stubGlobal('fetch', fetchMock)

    renderWithCatalogProviders(<ProgramsPage />, '/programs')

    expect(await screen.findByRole('heading', { name: 'Business Management Essentials' }, { timeout: 5_000 })).toBeInTheDocument()
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Next' }))

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/v1/programs?page=2&pageSize=6&status=Published'),
      expect.any(Object),
    )
  })

  it('renders a career path with its skills, programs, and courses', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse({
      id: 'path-1',
      title: 'Software Engineer',
      slug: 'software-engineer',
      description: 'Prepare for software engineering roles.',
      recommendedSkills: 'C#, Data Structures',
      status: 'Published',
      recommendedPrograms: [{
        programId: 'program-1',
        programTitle: 'Software Engineering Foundations',
        programSlug: 'software-engineering-foundations',
      }],
      recommendedCourses: [{
        courseId: 'course-1',
        courseTitle: 'Programming Fundamentals',
        courseSlug: 'programming-fundamentals',
      }],
    })))

    renderWithCatalogProviders(
      <Routes>
        <Route path="/career-paths/:slug" element={<CareerPathDetailPage />} />
      </Routes>,
      '/career-paths/software-engineer',
    )

    expect(await screen.findByRole('heading', { name: 'Software Engineer' }, { timeout: 5_000 })).toBeInTheDocument()
    expect(screen.getByText('C#')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Software Engineering Foundations' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Programming Fundamentals' })).toBeInTheDocument()
  })

  it('renders pagination for career paths', async () => {
    vi.stubGlobal('fetch', vi.fn(() => jsonResponse({
      items: [{
        id: 'path-1',
        title: 'Project Coordinator',
        slug: 'project-coordinator',
        description: 'Coordinate successful projects.',
        status: 'Published',
      }],
      page: 1,
      pageSize: 6,
      totalCount: 10,
      totalPages: 2,
    })))

    renderWithCatalogProviders(<CareerPathsPage />, '/career-paths')

    expect(await screen.findByRole('heading', { name: 'Project Coordinator' }, { timeout: 5_000 })).toBeInTheDocument()
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled()
  })
})
