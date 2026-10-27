import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import AttendancePage from './AttendancePage'

describe('AttendancePage', () => {
  beforeEach(() => {
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
    ])
  })

  it('shows every recorded session with status and note', async () => {
    render(<AttendancePage />)

    expect(await screen.findByRole('heading', { name: 'Network security lab' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Incident response' })).toBeInTheDocument()
    expect(screen.getByText('Present')).toBeInTheDocument()
    expect(screen.getByText('Late')).toBeInTheDocument()
    expect(screen.getByText('Completed the practical exercise.')).toBeInTheDocument()
  })
})
