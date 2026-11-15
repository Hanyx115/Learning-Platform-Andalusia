import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProfileContext } from '../../auth/profileContext'
import ProfileLayout from './ProfileLayout'

function renderProfileLayout(roles: string[]) {
  return render(
    <AuthProfileContext.Provider value={{
      isLoading: false,
      profile: {
        id: 'user-1',
        email: 'user@example.com',
        firstName: 'Demo',
        lastName: 'User',
        phoneNumber: '+201000000000',
        countryCode: 'EG',
        preferredLanguage: 'en',
        roles,
      },
    }}>
      <MemoryRouter initialEntries={['/profile']}>
        <Routes>
          <Route path="/profile" element={<ProfileLayout />}>
            <Route index element={<h1>Profile content</h1>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthProfileContext.Provider>,
  )
}

describe('ProfileLayout role navigation', () => {
  it('shows learner workspace links in the profile sidebar', () => {
    renderProfileLayout(['Learner'])
    const navigation = screen.getByRole('navigation', { name: 'Profile navigation' })

    expect(within(navigation).getByRole('link', { name: 'My applications' })).toHaveAttribute('href', '/profile/applications')
    expect(within(navigation).getByRole('link', { name: 'My schedule' })).toHaveAttribute('href', '/profile/schedule')
    expect(within(navigation).getByRole('link', { name: 'My payments' })).toHaveAttribute('href', '/profile/payments')
  })

  it('shows instructor workspace links in the profile sidebar', () => {
    renderProfileLayout(['Instructor'])
    const navigation = screen.getByRole('navigation', { name: 'Profile navigation' })

    expect(within(navigation).getByRole('link', { name: 'My sessions' })).toHaveAttribute('href', '/instructor/sessions')
    expect(within(navigation).getByRole('link', { name: 'My courses' })).toHaveAttribute('href', '/instructor/offerings')
    expect(within(navigation).queryByRole('link', { name: 'Users & roles' })).not.toBeInTheDocument()
  })

  it('shows content and administration links for an admin profile', () => {
    renderProfileLayout(['Admin'])
    const navigation = screen.getByRole('navigation', { name: 'Profile navigation' })

    expect(within(navigation).getByRole('link', { name: 'Catalog content' })).toHaveAttribute('href', '/cms/catalog')
    expect(within(navigation).getByRole('link', { name: 'Review applications' })).toHaveAttribute('href', '/staff/applications')
    expect(within(navigation).getByRole('link', { name: 'Users & roles' })).toHaveAttribute('href', '/admin/users')
  })
})
