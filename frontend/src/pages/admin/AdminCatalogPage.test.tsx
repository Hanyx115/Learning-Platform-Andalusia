import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { AuthProfileContext } from '../../auth/profileContext'
import AdminCatalogPage from './AdminCatalogPage'

const contentManagerProfile = {
  id: 'content-manager-1',
  email: 'content@example.com',
  firstName: 'Content',
  lastName: 'Manager',
  phoneNumber: '+201000000000',
  countryCode: 'EG',
  preferredLanguage: 'en',
  roles: ['ContentManager'],
}

describe('AdminCatalogPage role boundaries', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input)
      const body = url.includes('/course-categories')
        ? []
        : { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 }

      return Promise.resolve(new Response(JSON.stringify(body), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }))
    }))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('gives a Content Manager editorial forms without price or offering controls', async () => {
    const user = userEvent.setup()

    render(
      <AuthProfileContext.Provider value={{ profile: contentManagerProfile, isLoading: false }}>
        <MemoryRouter>
          <AdminCatalogPage />
        </MemoryRouter>
      </AuthProfileContext.Provider>,
    )

    const careerPathsTab = await screen.findByRole('tab', { name: /Career paths/ })
    await user.click(careerPathsTab)

    expect(screen.getByRole('heading', { name: 'Career paths' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Price (EGP)')).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Manage offerings' })).not.toBeInTheDocument()
  })

  it('shows one catalog section at a time and filters its items immediately', async () => {
    const user = userEvent.setup()
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input)
      const body = url.includes('/course-categories')
        ? [
            { id: 'category-1', name: 'Business and Management', slug: 'business-and-management' },
            { id: 'category-2', name: 'Technology', slug: 'technology' },
          ]
        : { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 }

      return Promise.resolve(new Response(JSON.stringify(body), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }))
    }))

    render(
      <AuthProfileContext.Provider value={{ profile: contentManagerProfile, isLoading: false }}>
        <MemoryRouter>
          <AdminCatalogPage />
        </MemoryRouter>
      </AuthProfileContext.Provider>,
    )

    expect(await screen.findByText('Business and Management')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Courses' })).not.toBeInTheDocument()

    await user.type(screen.getByRole('searchbox', { name: 'Find an item' }), 'tech')

    expect(screen.queryByText('Business and Management')).not.toBeInTheDocument()
    expect(screen.getByText('Technology')).toBeInTheDocument()
    expect(screen.getByText('Showing 1 of 2 items')).toBeInTheDocument()
  })
})
