import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import type { UpcomingSession } from '../../api/enrollment'
import SchedulePage from './SchedulePage'

const upcomingSession: UpcomingSession = {
  enrollmentId: 'enrollment-1',
  sessionId: 'session-1',
  courseOfferingId: 'offering-1',
  programOfferingId: null,
  offeringType: 'Course',
  offeringTitle: 'Computer Networks Fundamentals',
  sessionTitle: 'Network models and protocols',
  startsAtUtc: '2027-01-18T08:00:00Z',
  endsAtUtc: '2027-01-18T10:00:00Z',
  status: 'Scheduled',
  cancellationReason: null,
  instructorName: 'Mona Hassan',
  location: {
    id: 'location-1',
    branchName: 'Alexandria Campus',
    roomName: 'Technology Lab',
    city: 'Alexandria',
    address: '39 Khalil El-Khayat Basha, Sidi Gaber',
    mapUrl: null,
    capacity: 30,
    isActive: true,
  },
}

describe('SchedulePage', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'getMyUpcomingSessions').mockResolvedValue([
      upcomingSession,
      { ...upcomingSession, enrollmentId: 'enrollment-duplicate' },
      {
        ...upcomingSession,
        sessionId: 'session-2',
        courseOfferingId: 'offering-3',
        sessionTitle: 'Routing fundamentals',
        startsAtUtc: '2027-01-25T08:00:00Z',
      },
      {
        ...upcomingSession,
        enrollmentId: 'enrollment-2',
        sessionId: 'session-3',
        courseOfferingId: 'offering-2',
        offeringTitle: 'Cybersecurity Foundations',
        sessionTitle: 'Security principles',
      },
    ])
  })

  it('groups upcoming session details by course and removes duplicates', async () => {
    render(
      <MemoryRouter>
        <SchedulePage />
      </MemoryRouter>,
    )

    const networksGroup = await screen.findByRole('region', { name: 'Computer Networks Fundamentals' })
    const cybersecurityGroup = screen.getByRole('region', { name: 'Cybersecurity Foundations' })

    expect(within(networksGroup).getByText('2 sessions')).toBeInTheDocument()
    expect(within(networksGroup).getAllByRole('heading', { level: 3 })).toHaveLength(2)
    expect(within(networksGroup).getAllByText('Network models and protocols')).toHaveLength(1)
    expect(within(networksGroup).getAllByText('Alexandria Campus, Technology Lab - Alexandria')).toHaveLength(2)
    expect(within(networksGroup).getAllByText('Instructor: Mona Hassan')).toHaveLength(2)
    expect(within(cybersecurityGroup).getByText('1 session')).toBeInTheDocument()
  })
})
