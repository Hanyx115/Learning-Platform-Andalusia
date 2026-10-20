import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import type { Application, Payment } from '../../api/enrollment'
import ApplicationsPage from './ApplicationsPage'

const applications: Application[] = [{
    id: 'application-1',
    userId: 'user-1',
    courseOfferingId: 'offering-1',
    courseId: 'course-1',
    courseTitle: 'Patient Care Basics',
    programOfferingId: null,
    programId: null,
    programTitle: null,
    startsAtUtc: '2027-01-18T08:00:00Z',
    endsAtUtc: '2027-03-15T16:00:00Z',
    price: 750,
    currency: 'EGP',
    status: 'Accepted',
    submittedAt: '2026-10-20T08:00:00Z',
    reviewedAt: '2026-10-21T08:00:00Z',
    rejectionReason: null,
    statusHistory: [
      {
        id: 'history-1',
        status: 'Pending',
        changedAt: '2026-10-20T08:00:00Z',
        note: 'Application submitted.',
      },
      {
        id: 'history-2',
        status: 'Accepted',
        changedAt: '2026-10-21T08:00:00Z',
        note: null,
      },
    ],
  }]

describe('ApplicationsPage', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'getMyApplications').mockResolvedValue(applications)
    vi.spyOn(enrollmentApi, 'getMyPayments').mockResolvedValue([])
  })

  it('shows the selected intake and complete status history', async () => {
    render(
      <MemoryRouter>
        <ApplicationsPage />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Patient Care Basics' })).toBeInTheDocument()
    expect(screen.getByText('750 EGP')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Status history' })).toBeInTheDocument()
    expect(screen.getAllByText('Pending review')).toHaveLength(1)
    expect(screen.getAllByText('Accepted')).toHaveLength(2)
  })

  it('restores successful payment state after the page is reloaded', async () => {
    const payment: Payment = {
      id: 'payment-1',
      applicationId: 'application-1',
      amount: 750,
      currency: 'EGP',
      status: 'Succeeded',
      createdAt: '2026-10-21T09:00:00Z',
      completedAt: '2026-10-21T09:00:01Z',
    }
    vi.mocked(enrollmentApi.getMyPayments).mockResolvedValue([payment])

    render(
      <MemoryRouter>
        <ApplicationsPage />
      </MemoryRouter>,
    )

    expect(await screen.findByText('Payment successful — you are now enrolled.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'View my enrollments' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Pay now' })).not.toBeInTheDocument()
  })
})
