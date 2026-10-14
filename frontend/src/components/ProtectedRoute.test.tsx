import { render, screen } from '@testing-library/react'
import { afterEach } from 'vitest'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { authSession } from '../auth/session'
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
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z',
    })

    renderProtectedRoute()

    expect(screen.getByText('Protected profile')).toBeInTheDocument()
  })
})
