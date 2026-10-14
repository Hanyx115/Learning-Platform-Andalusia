import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import Header from './Header'
import { primaryNavItems } from '../navigation'

function renderHeader(initialEntries: string[] = ['/']) {
  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <Header />
    </MemoryRouter>,
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
})
