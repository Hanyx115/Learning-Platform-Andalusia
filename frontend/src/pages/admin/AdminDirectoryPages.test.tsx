import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import * as adminApi from '../../api/admin'
import AdminInstructorsPage from './AdminInstructorsPage'
import AdminStudentsPage from './AdminStudentsPage'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('admin directory pages', () => {
  it('searches, filters, and pages through student enrollments', async () => {
    const getStudentDirectory = vi.spyOn(adminApi, 'getStudentDirectory').mockResolvedValue({
      items: [{
        id: 'enrollment-1',
        userId: 'learner-1',
        learnerName: 'Demo Learner',
        email: 'learner@andalusia.test',
        applicationId: 'application-1',
        paymentId: 'payment-1',
        courseOfferingId: 'offering-1',
        courseId: 'course-1',
        courseTitle: 'Cybersecurity Foundations',
        programOfferingId: null,
        programId: null,
        programTitle: null,
        startsAtUtc: '2026-11-03T08:00:00Z',
        endsAtUtc: '2026-12-03T08:00:00Z',
        location: null,
        status: 'Active',
        enrolledAt: '2026-11-02T08:00:00Z',
        completedAt: null,
        cancelledAt: null,
      }],
      page: 1,
      pageSize: 10,
      totalCount: 12,
      totalPages: 2,
    })

    render(<MemoryRouter><AdminStudentsPage /></MemoryRouter>)

    expect(await screen.findByText('Demo Learner')).toBeInTheDocument()
    expect(getStudentDirectory).toHaveBeenCalledWith({
      page: 1, pageSize: 10, search: '', status: '', type: '',
    })

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search' }), 'cyber')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Enrollment status' }), 'Active')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Offering type' }), 'Course')
    await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }))

    await waitFor(() => expect(getStudentDirectory).toHaveBeenLastCalledWith({
      page: 1, pageSize: 10, search: 'cyber', status: 'Active', type: 'Course',
    }))

    await userEvent.click(screen.getByRole('button', { name: 'Next' }))
    await waitFor(() => expect(getStudentDirectory).toHaveBeenLastCalledWith({
      page: 2, pageSize: 10, search: 'cyber', status: 'Active', type: 'Course',
    }))
  })

  it('searches and filters instructors by account status', async () => {
    const getInstructorDirectory = vi.spyOn(adminApi, 'getInstructorDirectory').mockResolvedValue({
      items: [{
        id: 'instructor-1',
        email: 'instructor@andalusia.test',
        firstName: 'Demo',
        lastName: 'Instructor',
        phoneNumber: '+201000000000',
        isActive: true,
      }],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    })

    render(<MemoryRouter><AdminInstructorsPage /></MemoryRouter>)

    expect(await screen.findByText('Demo Instructor')).toBeInTheDocument()
    expect(screen.getByText('Active', { selector: 'span' })).toBeInTheDocument()

    await userEvent.type(screen.getByRole('searchbox', { name: 'Search' }), 'demo')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Account status' }), 'Active')
    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Items per page' }), '25')
    await userEvent.click(screen.getByRole('button', { name: 'Apply filters' }))

    await waitFor(() => expect(getInstructorDirectory).toHaveBeenLastCalledWith({
      page: 1, pageSize: 25, search: 'demo', status: 'Active',
    }))
  })
})
