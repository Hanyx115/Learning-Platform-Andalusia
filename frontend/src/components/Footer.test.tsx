import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import Footer from './Footer'
import { primaryNavItems } from '../navigation'

function renderFooter() {
  return render(
    <MemoryRouter>
      <Footer />
    </MemoryRouter>,
  )
}

describe('Footer', () => {
  it('renders a link for every primary navigation item', () => {
    renderFooter()

    const nav = screen.getByRole('navigation', { name: 'Footer navigation' })
    expect(within(nav).getAllByRole('link')).toHaveLength(primaryNavItems.length)
  })

  it('shows the current year in the copyright line', () => {
    renderFooter()

    const year = new Date().getFullYear().toString()
    expect(screen.getByText(new RegExp(year))).toBeInTheDocument()
  })
})
