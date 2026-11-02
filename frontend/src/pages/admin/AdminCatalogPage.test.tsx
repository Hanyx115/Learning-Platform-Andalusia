import { render, screen } from '@testing-library/react'
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
    render(
      <AuthProfileContext.Provider value={{ profile: contentManagerProfile, isLoading: false }}>
        <MemoryRouter>
          <AdminCatalogPage />
        </MemoryRouter>
      </AuthProfileContext.Provider>,
    )

    expect(await screen.findByRole('heading', { name: 'Career paths' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Price (EGP)')).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Manage offerings' })).not.toBeInTheDocument()
  })
})
