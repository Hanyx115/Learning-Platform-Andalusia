import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, vi } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { authSession } from '../../auth/session'
import LoginPage from './LoginPage'

describe('LoginPage', () => {
  afterEach(() => {
    authSession.clear()
    vi.unstubAllGlobals()
  })

  it('stores the returned session and continues to the requested protected page', async () => {
    const user = userEvent.setup()
    const tokens = {
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z',
    }

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(tokens), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    })))

    render(
      <MemoryRouter initialEntries={[{ pathname: '/login', state: { from: '/profile' } }]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/profile" element={<p>Profile destination</p>} />
        </Routes>
      </MemoryRouter>,
    )

    await user.type(screen.getByLabelText('Email address'), 'learner@example.com')
    await user.type(screen.getByLabelText('Password'), 'Strong!123')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByText('Profile destination')).toBeInTheDocument()
    expect(authSession.getTokens()).toEqual(tokens)
  })
})
