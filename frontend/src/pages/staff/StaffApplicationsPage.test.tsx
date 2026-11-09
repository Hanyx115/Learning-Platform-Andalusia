import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import type { Application } from '../../api/enrollment'
import StaffApplicationsPage from './StaffApplicationsPage'

const application: Application = {
  id: 'application-1',
  userId: 'learner-1',
  applicantFirstName: 'Nour',
  applicantLastName: 'Hassan',
  applicantEmail: 'nour@andalusia.test',
  courseOfferingId: 'offering-1',
  courseId: 'course-1',
  courseTitle: 'Clinical Communication Skills',
  programOfferingId: null,
  programId: null,
  programTitle: null,
  startsAtUtc: '2027-01-10T08:00:00Z',
  endsAtUtc: '2027-02-10T08:00:00Z',
  price: 750,
  currency: 'EGP',
  status: 'Pending',
  submittedAt: '2026-10-22T08:00:00Z',
  reviewedAt: null,
  rejectionReason: null,
  statusHistory: [],
}

describe('StaffApplicationsPage', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'getApplicationsPage').mockResolvedValue({
      items: [application],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('shows applicant and intake details with a read-only profile link', async () => {
    render(<MemoryRouter><StaffApplicationsPage /></MemoryRouter>)

    expect(await screen.findByText('Nour Hassan')).toBeInTheDocument()
    expect(screen.getByText('nour@andalusia.test')).toBeInTheDocument()
    expect(screen.getByText('Clinical Communication Skills')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'View profile' })).toHaveAttribute(
      'href',
      '/staff/applicants/learner-1',
    )
  })

  it('searches, sorts, filters by intake, and changes pages', async () => {
    const getPage = vi.mocked(enrollmentApi.getApplicationsPage)
    getPage.mockResolvedValue({
      items: [application],
      page: 1,
      pageSize: 25,
      totalCount: 30,
      totalPages: 2,
    })

    render(<MemoryRouter><StaffApplicationsPage /></MemoryRouter>)
    await screen.findByText('Nour Hassan')

    await userEvent.type(screen.getByLabelText('Learner name'), 'Nour')
    await userEvent.type(screen.getByLabelText('Course or program'), 'Clinical')
    await userEvent.type(screen.getByLabelText('Intake starts from'), '2027-01-01')
    await userEvent.type(screen.getByLabelText('Intake starts before'), '2027-01-31')
    await userEvent.selectOptions(screen.getByLabelText('Sort by'), 'offering_asc')
    await userEvent.selectOptions(screen.getByLabelText('Items per page'), '25')
    await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }))

    await waitFor(() => expect(getPage).toHaveBeenLastCalledWith({
      status: 'Pending',
      page: 1,
      pageSize: 25,
      learnerName: 'Nour',
      offeringTitle: 'Clinical',
      intakeFrom: '2027-01-01',
      intakeTo: '2027-01-31',
      sort: 'offering_asc',
    }))

    await userEvent.click(screen.getByRole('button', { name: 'Next' }))
    await waitFor(() => expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({
      page: 2,
      pageSize: 25,
    })))
  })

  it('rejects with a guided quick reason', async () => {
    const reject = vi.spyOn(enrollmentApi, 'rejectApplication').mockResolvedValue({
      ...application,
      status: 'Rejected',
      rejectionReason: 'This intake has reached full capacity. Please choose another available intake.',
    })

    render(<MemoryRouter><StaffApplicationsPage /></MemoryRouter>)

    await userEvent.click(await screen.findByRole('button', { name: 'Reject application' }))
    await userEvent.click(screen.getByRole('radio', { name: /Full capacity reached/i }))
    await userEvent.click(screen.getByRole('button', { name: 'Confirm rejection' }))

    await waitFor(() => expect(reject).toHaveBeenCalledWith(
      application.id,
      'This intake has reached full capacity. Please choose another available intake.',
    ))
  })

  it('opens a focused text area for an other reason', async () => {
    const reject = vi.spyOn(enrollmentApi, 'rejectApplication').mockResolvedValue({
      ...application,
      status: 'Rejected',
      rejectionReason: 'Please upload the requested certificate.',
    })

    render(<MemoryRouter><StaffApplicationsPage /></MemoryRouter>)

    await userEvent.click(await screen.findByRole('button', { name: 'Reject application' }))
    await userEvent.click(screen.getByRole('radio', { name: /Other reason/i }))
    await userEvent.type(screen.getByRole('textbox', { name: 'Write a clear reason' }), 'Please upload the requested certificate.')
    await userEvent.click(screen.getByRole('button', { name: 'Confirm rejection' }))

    await waitFor(() => expect(reject).toHaveBeenCalledWith(
      application.id,
      'Please upload the requested certificate.',
    ))
  })
})
