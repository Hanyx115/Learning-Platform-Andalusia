import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AuthProfileContext } from '../../auth/profileContext'
import CmsLayout from './CmsLayout'

function renderLayout(roles: string[]) {
  return render(
    <AuthProfileContext.Provider value={{
      isLoading: false,
      profile: {
        id: 'staff-1',
        email: 'staff@example.com',
        firstName: 'Staff',
        lastName: 'Member',
        phoneNumber: '+201000000000',
        countryCode: 'EG',
        preferredLanguage: 'en',
        roles,
      },
    }}>
      <MemoryRouter initialEntries={['/cms/catalog']}>
        <Routes>
          <Route path="/cms" element={<CmsLayout />}>
            <Route path="catalog" element={<h1>Catalog</h1>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthProfileContext.Provider>,
  )
}

describe('CmsLayout role navigation', () => {
  it('shows a Manager the catalog link but not website-content controls', () => {
    renderLayout(['Manager'])
    const navigation = screen.getByRole('navigation', { name: 'Content dashboard navigation' })

    expect(within(navigation).getByRole('link', { name: 'Catalog content' })).toBeInTheDocument()
    expect(within(navigation).queryByRole('link', { name: 'Website content' })).not.toBeInTheDocument()
    expect(within(navigation).queryByRole('link', { name: 'Notifications' })).not.toBeInTheDocument()
  })

  it('shows a Content Manager all editorial navigation', () => {
    renderLayout(['ContentManager'])
    const navigation = screen.getByRole('navigation', { name: 'Content dashboard navigation' })

    expect(within(navigation).getByRole('link', { name: 'Catalog content' })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: 'Website content' })).toBeInTheDocument()
    expect(within(navigation).getByRole('link', { name: 'Notifications' })).toBeInTheDocument()
  })
})
