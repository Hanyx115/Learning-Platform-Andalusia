import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as compensationApi from '../../api/compensation'
import { AuthProfileContext } from '../../auth/profileContext'
import AdminCompensationPage from './AdminCompensationPage'

afterEach(() => vi.restoreAllMocks())

describe('AdminCompensationPage', () => {
  it('shows instructor rates, manager monthly rates and the approval queue to an Admin', async () => {
    vi.spyOn(compensationApi, 'getInstructorCompensationTargets').mockResolvedValue([{
      rateId: 'rate-1', instructorUserId: 'instructor-1', instructorName: 'Demo Instructor',
      courseOfferingId: 'offering-1', courseTitle: 'Cybersecurity Foundations',
      startsAtUtc: '2026-08-01T08:00:00Z', endsAtUtc: '2026-10-01T08:00:00Z',
      offeringStatus: 'Closed', amount: 1200, currency: 'EGP',
      isEligibleForPayout: true, payoutStatus: null,
    }])
    vi.spyOn(compensationApi, 'getStaffCompensationRates').mockResolvedValue([{
      userId: 'manager-1', staffName: 'Demo Manager', role: 'Manager', monthlyAmount: 5000, currency: 'EGP',
    }])
    vi.spyOn(compensationApi, 'getCompensationPayouts').mockResolvedValue([{
      id: 'payout-1', userId: 'instructor-1', payeeName: 'Demo Instructor',
      payoutType: 'InstructorCourse', description: 'Completed course: Cybersecurity Foundations',
      amount: 1200, currency: 'EGP', status: 'Reviewed', createdAtUtc: '2026-11-02T10:00:00Z',
      reviewedAtUtc: '2026-11-02T10:01:00Z', approvedAtUtc: null,
    }])

    render(
      <AuthProfileContext.Provider value={{
        profile: {
          id: 'admin-1', email: 'admin@example.com', firstName: 'Admin', lastName: 'User',
          phoneNumber: '+201000000000', countryCode: 'EG', preferredLanguage: 'en', roles: ['Admin'],
        },
        isLoading: false,
      }}>
        <AdminCompensationPage />
      </AuthProfileContext.Provider>,
    )

    expect(await screen.findByRole('heading', { name: 'Compensation management' })).toBeInTheDocument()
    expect(screen.getAllByText('Demo Instructor').length).toBeGreaterThan(0)
    expect(screen.getByText('Demo Manager')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Approve and credit balance' })).toBeInTheDocument()
  })
})
