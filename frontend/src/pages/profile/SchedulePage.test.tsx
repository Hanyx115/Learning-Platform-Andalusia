import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import type { UpcomingSession } from '../../api/enrollment'
import SchedulePage from './SchedulePage'

const upcomingSession: UpcomingSession = {
  enrollmentId: 'enrollment-1',
  sessionId: 'session-1',
  courseOfferingId: 'offering-1',
  courseTitle: 'Computer Networks Fundamentals',
  sessionTitle: 'Network models and protocols',
  startsAtUtc: '2027-01-18T08:00:00Z',
  endsAtUtc: '2027-01-18T10:00:00Z',
  instructorName: 'Mona Hassan',
  location: {
    id: 'location-1',
    branchName: 'Alexandria Campus',
    roomName: 'Technology Lab',
    city: 'Alexandria',
    address: '39 Khalil El-Khayat Basha, Sidi Gaber',
    mapUrl: null,
    isActive: true,
  },
}

describe('SchedulePage', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'getMyUpcomingSessions').mockResolvedValue([upcomingSession])
  })

  it('shows the enrolled learner their upcoming session details', async () => {
    render(
      <MemoryRouter>
        <SchedulePage />
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Computer Networks Fundamentals' })).toBeInTheDocument()
    expect(screen.getByText('Network models and protocols')).toBeInTheDocument()
    expect(screen.getByText('Alexandria Campus, Technology Lab - Alexandria')).toBeInTheDocument()
    expect(screen.getByText('Instructor: Mona Hassan')).toBeInTheDocument()
  })
})
