import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, vi } from 'vitest'
import CareerPathDetailPage from './CareerPathDetailPage'
import CareerPathsPage from './CareerPathsPage'
import CourseDetailPage from './CourseDetailPage'
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
        categorySlug: 'computer-engineering',
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

  it('submits every course filter with the category slug and resets the form', async () => {
    const fetchMock = vi.fn(() => jsonResponse({
      items: [{
        id: 'course-1',
        title: 'Business Communication Essentials',
        slug: 'business-communication-essentials',
        description: 'Practical professional communication.',
        status: 'Published',
        type: 'Hybrid',
        categorySlug: 'business-management',
        categoryName: 'Business and Management',
        createdAt: '2026-07-01T00:00:00Z',
      }],
      page: 1,
      pageSize: 6,
      totalCount: 1,
      totalPages: 1,
    }))
    vi.stubGlobal('fetch', fetchMock)

    renderWithCatalogProviders(<CoursesPage />, '/courses')

    await screen.findByRole('heading', { name: 'Business Communication Essentials' })
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Category' }), 'business-management')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Course type' }), 'Hybrid')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Sort by' }), 'title-desc')
    await userEvent.type(
      screen.getByRole('searchbox', { name: 'Search' }),
      'Business Communication Essential',
    )
    await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }))

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining(
        'search=Business+Communication+Essential&category=business-management&type=Hybrid&sort=title-desc',
      ),
      expect.any(Object),
    )

    await userEvent.click(screen.getByRole('link', { name: 'Reset' }))
    expect(screen.getByRole('searchbox', { name: 'Search' })).toHaveValue('')
    expect(screen.getByRole('combobox', { name: 'Category' })).toHaveValue('')
    expect(screen.getByRole('combobox', { name: 'Course type' })).toHaveValue('')
    expect(screen.getByRole('combobox', { name: 'Sort by' })).toHaveValue('title')
  })

  it('shows available course intakes to visitors before asking them to sign in', async () => {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.endsWith('/offerings')) {
        return jsonResponse([{
          id: 'offering-1',
          startsAtUtc: '2027-01-18T08:00:00Z',
          endsAtUtc: '2027-03-15T16:00:00Z',
          applicationOpensAtUtc: '2026-01-01T00:00:00Z',
          applicationClosesAtUtc: '2027-01-10T23:59:59Z',
          price: 750,
          currency: 'EGP',
          capacity: 25,
          status: 'Open',
          location: {
            id: 'location-1',
            branchName: 'Alexandria Campus',
            roomName: 'Training Room A',
            city: 'Alexandria',
            address: '39 Khalil El-Khayat Basha, Sidi Gaber',
            mapUrl: null,
            isActive: true,
          },
        }])
      }

      if (url.endsWith('/sessions')) {
        return jsonResponse([{
          id: 'session-1',
          courseOfferingId: 'offering-1',
          courseTitle: 'Patient Care Basics',
          title: 'Session 1',
          startsAtUtc: '2027-01-18T09:00:00Z',
          endsAtUtc: '2027-01-18T11:00:00Z',
          status: 'Scheduled',
          cancellationReason: null,
          instructorUserId: null,
          instructorName: null,
          location: {
            id: 'location-1',
            branchName: 'Alexandria Campus',
            roomName: 'Training Room A',
            city: 'Alexandria',
            address: '39 Khalil El-Khayat Basha, Sidi Gaber',
            mapUrl: null,
            isActive: true,
          },
        }])
      }

      return jsonResponse({
        id: 'course-1',
        title: 'Patient Care Basics',
        slug: 'patient-care-basics',
        description: 'Learn patient care fundamentals.',
        status: 'Published',
        type: 'Offline',
        price: null,
        categorySlug: 'clinical-care',
        categoryName: 'Clinical Care',
        createdAt: '2026-01-01T00:00:00Z',
      })
    }))

    renderWithCatalogProviders(
      <Routes>
        <Route path="/courses/:slug" element={<CourseDetailPage />} />
      </Routes>,
      '/courses/patient-care-basics',
    )

    expect(await screen.findByRole('heading', { name: 'Patient Care Basics' })).toBeInTheDocument()
    expect(await screen.findByText('750 EGP')).toBeInTheDocument()
    expect(screen.getByText('25 seats')).toBeInTheDocument()
    expect(screen.getAllByText('Alexandria Campus, Training Room A - Alexandria')).toHaveLength(2)
    expect(await screen.findByText('Session 1')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sign in to apply' })).toHaveAttribute('href', '/login')
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

  it('applies and resets program search and sorting', async () => {
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
      totalCount: 1,
      totalPages: 1,
    }))
    vi.stubGlobal('fetch', fetchMock)

    renderWithCatalogProviders(<ProgramsPage />, '/programs')
    await screen.findByRole('heading', { name: 'Business Management Essentials' })

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search' }), 'Business')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Sort by' }), 'title-desc')
    await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }))

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/v1/programs?page=1&pageSize=6&status=Published&search=Business&sort=title-desc'),
      expect.any(Object),
    )

    await userEvent.click(screen.getByRole('link', { name: 'Reset' }))
    expect(screen.getByRole('searchbox', { name: 'Search' })).toHaveValue('')
    expect(screen.getByRole('combobox', { name: 'Sort by' })).toHaveValue('title')
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

  it('applies and resets career-path search and sorting', async () => {
    const fetchMock = vi.fn(() => jsonResponse({
      items: [{
        id: 'path-1',
        title: 'Project Coordinator',
        slug: 'project-coordinator',
        description: 'Coordinate successful projects.',
        status: 'Published',
      }],
      page: 1,
      pageSize: 6,
      totalCount: 1,
      totalPages: 1,
    }))
    vi.stubGlobal('fetch', fetchMock)

    renderWithCatalogProviders(<CareerPathsPage />, '/career-paths')
    await screen.findByRole('heading', { name: 'Project Coordinator' })

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search' }), 'Project')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Sort by' }), 'title-desc')
    await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }))

    expect(fetchMock).toHaveBeenCalledWith(
      expect.stringContaining('/api/v1/career-paths?page=1&pageSize=6&status=Published&search=Project&sort=title-desc'),
      expect.any(Object),
    )

    await userEvent.click(screen.getByRole('link', { name: 'Reset' }))
    expect(screen.getByRole('searchbox', { name: 'Search' })).toHaveValue('')
    expect(screen.getByRole('combobox', { name: 'Sort by' })).toHaveValue('title')
  })
})
