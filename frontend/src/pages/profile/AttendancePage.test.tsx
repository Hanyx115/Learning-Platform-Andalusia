import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import AttendancePage from './AttendancePage'

describe('AttendancePage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.spyOn(enrollmentApi, 'getMyAttendance').mockResolvedValue([
      {
        sessionId: 'session-1',
        sessionTitle: 'Network security lab',
        courseTitle: 'Cybersecurity Foundations',
        programTitle: null,
        startsAtUtc: '2027-02-10T08:00:00Z',
        status: 'Present',
        note: 'Completed the practical exercise.',
      },
      {
        sessionId: 'session-2',
        sessionTitle: 'Incident response',
        courseTitle: 'Cybersecurity Foundations',
        programTitle: null,
        startsAtUtc: '2027-02-12T08:00:00Z',
        status: 'Late',
        note: null,
      },
      {
        sessionId: 'session-3',
        sessionTitle: 'Routing fundamentals',
        courseTitle: 'Computer Networks Fundamentals',
        programTitle: null,
        startsAtUtc: '2027-02-15T08:00:00Z',
        status: 'Absent',
        note: null,
      },
    ])
  })

  it('groups every attendance record by course with status and note', async () => {
    render(<AttendancePage />)

    const cybersecurityGroup = await screen.findByRole('region', { name: 'Cybersecurity Foundations' })
    const networksGroup = screen.getByRole('region', { name: 'Computer Networks Fundamentals' })

    expect(within(cybersecurityGroup).getByText('2 attendance records')).toBeInTheDocument()
    expect(within(cybersecurityGroup).getByRole('heading', { name: 'Network security lab' })).toBeInTheDocument()
    expect(within(cybersecurityGroup).getByRole('heading', { name: 'Incident response' })).toBeInTheDocument()
    expect(within(cybersecurityGroup).getByText('Present')).toBeInTheDocument()
    expect(within(cybersecurityGroup).getByText('Late')).toBeInTheDocument()
    expect(within(cybersecurityGroup).getByText('Completed the practical exercise.')).toBeInTheDocument()
    expect(within(networksGroup).getByText('1 attendance record')).toBeInTheDocument()
  })

  it('lets the learner refresh attendance without leaving the page', async () => {
    const attendanceRequest = vi.mocked(enrollmentApi.getMyAttendance)
    attendanceRequest
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([{
        sessionId: 'new-session',
        sessionTitle: 'Newly marked session',
        courseTitle: 'Cybersecurity Foundations',
        programTitle: null,
        startsAtUtc: '2027-02-20T08:00:00Z',
        status: 'Present',
        note: null,
      }])

    render(<AttendancePage />)
    expect(await screen.findByText('No attendance has been recorded yet.')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Refresh attendance' }))

    expect(await screen.findByRole('heading', { name: 'Newly marked session' })).toBeInTheDocument()
    expect(attendanceRequest).toHaveBeenCalledTimes(2)
  })
})
