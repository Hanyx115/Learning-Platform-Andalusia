import { render, screen } from '@testing-library/react'
import { afterEach } from 'vitest'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { authSession } from '../auth/session'
import { AuthProfileContext } from '../auth/profileContext'
import ProtectedRoute from './ProtectedRoute'

function LoginDestination() {
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from
  return <p>Login destination: {from}</p>
}

function renderProtectedRoute() {
  return render(
    <MemoryRouter initialEntries={['/profile?tab=details']}>
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="/profile" element={<p>Protected profile</p>} />
        </Route>
        <Route path="/login" element={<LoginDestination />} />
      </Routes>
    </MemoryRouter>,
  )
}

function renderRoleProtectedRoute(roles: string[], allowedRoles: string[], isLoading = false) {
  authSession.setTokens({
    accessToken: 'access-token',
    accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
    refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z',
  })

  return render(
    <AuthProfileContext.Provider value={{
      isLoading,
      profile: isLoading ? null : {
        id: 'user-1',
        email: 'user@example.com',
        firstName: 'Test',
        lastName: 'User',
        phoneNumber: '+201234567890',
        countryCode: 'EG',
        preferredLanguage: 'en',
        roles,
      },
    }}>
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route element={<ProtectedRoute allowedRoles={allowedRoles} />}>
            <Route path="/admin" element={<p>Admin dashboard</p>} />
          </Route>
          <Route path="/forbidden" element={<p>Forbidden page</p>} />
        </Routes>
      </MemoryRouter>
    </AuthProfileContext.Provider>,
  )
}

describe('ProtectedRoute', () => {
  afterEach(() => {
    authSession.clear()
  })

  it('redirects signed-out visitors to login and keeps their intended destination', () => {
    authSession.clear()
    renderProtectedRoute()

    expect(screen.getByText('Login destination: /profile?tab=details')).toBeInTheDocument()
  })

  it('renders the protected page when a session exists', () => {
    authSession.setTokens({
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z',
    })

    renderProtectedRoute()

    expect(screen.getByText('Protected profile')).toBeInTheDocument()
  })

  it('renders a role-protected page for an allowed role', () => {
    renderRoleProtectedRoute(['Admin'], ['Admin'])

    expect(screen.getByText('Admin dashboard')).toBeInTheDocument()
  })

  it('redirects an authenticated user without the required role', () => {
    renderRoleProtectedRoute(['Learner'], ['Admin'])

    expect(screen.getByText('Forbidden page')).toBeInTheDocument()
  })

  it('waits for the profile before deciding role access', () => {
    renderRoleProtectedRoute([], ['Admin'], true)

    expect(screen.getByRole('status')).toHaveTextContent('Loading your profile')
  })
})
