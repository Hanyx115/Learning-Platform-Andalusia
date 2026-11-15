import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as authApi from '../../api/auth'
import StaffApplicantProfilePage from './StaffApplicantProfilePage'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('StaffApplicantProfilePage', () => {
  it('shows the applicant profile as read-only information', async () => {
    vi.spyOn(authApi, 'getProfileByUserId').mockResolvedValue({
      id: 'learner-1',
      email: 'nour@andalusia.test',
      firstName: 'Nour',
      lastName: 'Hassan',
      phoneNumber: '+201000000000',
      countryCode: 'EG',
      preferredLanguage: 'en',
      roles: ['Learner'],
    })

    render(
      <MemoryRouter initialEntries={['/staff/applicants/learner-1']}>
        <Routes>
          <Route path="/staff/applicants/:userId" element={<StaffApplicantProfilePage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Nour Hassan' })).toBeInTheDocument()
    expect(screen.getByText('+201000000000')).toBeInTheDocument()
    expect(screen.getByText('Read-only')).toBeInTheDocument()
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back to applications' })).toHaveAttribute('href', '/staff/applications')
  })
})
