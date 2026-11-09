import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as instructorApi from '../../api/instructor'
import InstructorSessionAttendancePage from './InstructorSessionAttendancePage'

describe('InstructorSessionAttendancePage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  it('submits an empty status when an instructor clears existing attendance', async () => {
    const existingRecord: instructorApi.SessionAttendanceRecord = {
      userId: 'learner-1',
      learnerName: 'Demo Learner',
      email: 'learner@example.com',
      status: 'Present',
      note: 'Was present',
      recordedByUserId: 'instructor-1',
      recordedAtUtc: '2026-11-05T08:00:00Z',
    }
    vi.spyOn(instructorApi, 'getSessionAttendance').mockResolvedValue([existingRecord])
    const saveAttendance = vi.spyOn(instructorApi, 'markSessionAttendance').mockResolvedValue([
      { ...existingRecord, status: null, note: null, recordedByUserId: null, recordedAtUtc: null },
    ])

    render(
      <MemoryRouter initialEntries={['/instructor/sessions/session-1/attendance?type=Course']}>
        <Routes>
          <Route
            path="/instructor/sessions/:sessionId/attendance"
            element={<InstructorSessionAttendancePage />}
          />
        </Routes>
      </MemoryRouter>,
    )

    await userEvent.selectOptions(await screen.findByLabelText('Attendance'), '')
    await userEvent.click(screen.getByRole('button', { name: 'Save attendance' }))

    await waitFor(() => expect(saveAttendance).toHaveBeenCalledWith(
      'session-1',
      'Course',
      [{ userId: 'learner-1', status: null, note: null }],
    ))
    expect(await screen.findByText('Attendance has been saved.')).toBeInTheDocument()
  })
})
