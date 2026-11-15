import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as instructorApi from '../../api/instructor'
import InstructorOverviewPage from './InstructorOverviewPage'

describe('InstructorOverviewPage', () => {
  beforeEach(() => {
    vi.spyOn(instructorApi, 'getMyInstructorOverview').mockResolvedValue({
      assignedOfferings: 2,
      activeStudents: 24,
      upcomingSessions: 3,
      pendingSubmissions: 5,
      nextSessions: [{
        sessionId: 'session-1',
        courseOfferingId: 'offering-1',
        programOfferingId: null,
        offeringType: 'Course',
        offeringTitle: 'Cybersecurity Foundations',
        sessionTitle: 'Secure network design',
        startsAtUtc: '2026-11-05T08:00:00Z',
        endsAtUtc: '2026-11-05T10:00:00Z',
        status: 'Scheduled',
        location: null,
      }],
    })
  })

  it('shows teaching KPIs and the next assigned session', async () => {
    render(<InstructorOverviewPage />)

    expect(await screen.findByRole('heading', { name: 'Instructor overview' })).toBeInTheDocument()
    expect(screen.getByText('Secure network design')).toBeInTheDocument()
    expect(screen.getByText('Cybersecurity Foundations')).toBeInTheDocument()
    expect(screen.getByText('Submissions awaiting grades')).toBeInTheDocument()
  })
})
