import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import Header from './Header'
import { primaryNavItems } from '../navigation'
import { authSession } from '../auth/session'
import { AuthProfileContext } from '../auth/profileContext'

afterEach(() => {
  authSession.clear()
})

function renderHeader(initialEntries: string[] = ['/'], roles: string[] = []) {
  const profile = roles.length > 0 ? {
    id: 'user-1',
    email: 'user@example.com',
    firstName: 'Demo',
    lastName: 'User',
    phoneNumber: '+201000000000',
    countryCode: 'EG',
    preferredLanguage: 'en',
    roles,
  } : null

  return render(
    <AuthProfileContext.Provider value={{ profile, isLoading: false }}>
      <MemoryRouter initialEntries={initialEntries}>
        <Header />
      </MemoryRouter>
    </AuthProfileContext.Provider>,
  )
}

describe('Header', () => {
  it('renders every primary navigation item and signed-out authentication actions', () => {
    renderHeader()

    const nav = screen.getByRole('navigation', { name: 'Primary navigation' })
    expect(within(nav).getAllByRole('link')).toHaveLength(primaryNavItems.length + 2)
    expect(within(nav).getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
    expect(within(nav).getByRole('link', { name: 'Create account' })).toHaveAttribute('href', '/register')
  })

  it('marks the current route as the active link', () => {
    renderHeader(['/about'])

    expect(screen.getByRole('link', { name: 'About' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('link', { name: 'Home' })).not.toHaveAttribute('aria-current')
  })

  it('toggles the mobile menu open and closed', async () => {
    const user = userEvent.setup()
    renderHeader()

    const toggle = screen.getByRole('button', { name: 'Open menu' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')

    await user.click(toggle)
    expect(screen.getByRole('button', { name: 'Close menu' })).toHaveAttribute('aria-expanded', 'true')

    await user.click(screen.getByRole('button', { name: 'Close menu' }))
    expect(screen.getByRole('button', { name: 'Open menu' })).toHaveAttribute('aria-expanded', 'false')
  })

  it('closes the mobile menu on Escape and returns focus to the toggle button', async () => {
    const user = userEvent.setup()
    renderHeader()

    const toggle = screen.getByRole('button', { name: 'Open menu' })
    await user.click(toggle)
    expect(screen.getByRole('button', { name: 'Close menu' })).toHaveAttribute('aria-expanded', 'true')

    await user.keyboard('{Escape}')

    const closedToggle = screen.getByRole('button', { name: 'Open menu' })
    expect(closedToggle).toHaveAttribute('aria-expanded', 'false')
    expect(closedToggle).toHaveFocus()
  })

  it('closes the mobile menu when a navigation link is clicked', async () => {
    const user = userEvent.setup()
    renderHeader()

    await user.click(screen.getByRole('button', { name: 'Open menu' }))
    await user.click(screen.getByRole('link', { name: 'About' }))

    expect(screen.getByRole('button', { name: 'Open menu' })).toHaveAttribute('aria-expanded', 'false')
  })

  it('moves authenticated actions into an account drawer', async () => {
    const user = userEvent.setup()
    authSession.setTokens({
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2099-01-02T00:00:00Z',
    })
    renderHeader()

    const accountToggle = screen.getByRole('button', { name: 'Account' })
    expect(accountToggle).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

    await user.click(accountToggle)

    expect(accountToggle).toHaveAttribute('aria-expanded', 'true')
    const drawer = screen.getByRole('dialog', { name: 'Andalusia Academy' })
    const accountNavigation = within(drawer).getByRole('navigation', { name: 'Account navigation' })
    expect(within(accountNavigation).getByRole('link', { name: 'My profile' })).toHaveAttribute('href', '/profile')
    expect(within(accountNavigation).getByRole('link', { name: 'Notifications' })).toHaveAttribute('href', '/profile/notifications')
    expect(within(drawer).getByRole('button', { name: 'Sign out' })).toBeInTheDocument()
  })

  it('closes the account drawer on Escape and restores focus', async () => {
    const user = userEvent.setup()
    authSession.setTokens({
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2099-01-02T00:00:00Z',
    })
    renderHeader()

    const accountToggle = screen.getByRole('button', { name: 'Account' })
    await user.click(accountToggle)
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(accountToggle).toHaveAttribute('aria-expanded', 'false')
    expect(accountToggle).toHaveFocus()
  })

  it('shows every workspace link allowed by the signed-in user roles', async () => {
    const user = userEvent.setup()
    authSession.setTokens({
      accessToken: 'access-token',
      accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
      refreshToken: 'refresh-token',
      refreshTokenExpiresAtUtc: '2099-01-02T00:00:00Z',
    })
    renderHeader(['/profile'], ['Learner', 'Instructor', 'ContentManager', 'Manager', 'Admin'])

    await user.click(screen.getByRole('button', { name: 'Account' }))

    const navigation = screen.getByRole('navigation', { name: 'Account navigation' })
    expect(within(navigation).getByRole('link', { name: 'My applications' })).toHaveAttribute('href', '/profile/applications')
    expect(within(navigation).getByRole('link', { name: 'My sessions' })).toHaveAttribute('href', '/instructor/sessions')
    expect(within(navigation).getByRole('link', { name: 'Catalog content' })).toHaveAttribute('href', '/cms/catalog')
    expect(within(navigation).getByRole('link', { name: 'Review applications' })).toHaveAttribute('href', '/staff/applications')
    expect(within(navigation).getByRole('link', { name: 'Users & roles' })).toHaveAttribute('href', '/admin/users')
  })
})
