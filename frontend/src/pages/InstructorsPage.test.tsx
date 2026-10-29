import { render, screen } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import InstructorsPage from './InstructorsPage'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('InstructorsPage', () => {
  it('renders published instructors as team members with professional links', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(JSON.stringify([{
      id: 'profile-1',
      contentType: 'InstructorProfile',
      key: 'demo-instructor',
      titleEn: 'Instructor Demo',
      titleAr: 'المدرب التجريبي',
      bodyEn: 'Practical technology instructor.',
      bodyAr: 'مدرب تقني عملي.',
      imageUrl: null,
      linkUrl: 'https://www.linkedin.com/company/andalusia-academy',
      relatedUserId: null,
      displayOrder: 10,
      isPublished: true,
      createdAtUtc: '2026-10-28T00:00:00Z',
      updatedAtUtc: '2026-10-28T00:00:00Z',
    }]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }))))

    render(<InstructorsPage />)

    expect(screen.getByRole('heading', { level: 1, name: 'Meet our team' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { level: 2, name: 'Instructor Demo' })).toBeInTheDocument()
    expect(screen.getByText('Practical technology instructor.')).toBeInTheDocument()
    expect(screen.getByText('ID')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'LinkedIn profile' })).toHaveAttribute(
      'href',
      'https://www.linkedin.com/company/andalusia-academy',
    )
  })
})
