import { act, fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import AboutPage from './AboutPage'
import ContactPage from './ContactPage'
import ForBusinessPage from './ForBusinessPage'

describe('public information pages', () => {
  it('renders the About page with real academy information', () => {
    render(
      <MemoryRouter>
        <AboutPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('heading', { level: 1, name: 'Learning that supports professional growth' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Educational excellence' })).toBeInTheDocument()
  })

  it('renders accessible contact details and message fields', () => {
    render(
      <MemoryRouter>
        <ContactPage />
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: '01202317780' })).toHaveAttribute('href', 'tel:+201202317780')
    expect(screen.getByRole('link', { name: 'andalusiaacadem@andalusia.com' })).toHaveAttribute(
      'href',
      'mailto:andalusiaacadem@andalusia.com',
    )
    expect(screen.getByRole('textbox', { name: 'Message' })).toBeRequired()
    expect(screen.getByRole('button', { name: 'Prepare email' })).toHaveAttribute('type', 'submit')
  })

  it('presents partner companies in an accessible carousel', () => {
    vi.useFakeTimers()

    try {
      render(
        <MemoryRouter>
          <ForBusinessPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('region', { name: 'Companies we work with' })).toBeInTheDocument()
      expect(screen.getByText('Meta')).toBeInTheDocument()

      act(() => vi.advanceTimersByTime(4000))
      expect(screen.getByText('Apple')).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: 'Show next company' }))
      expect(screen.getByText('Amazon')).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })
})
