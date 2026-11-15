import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as adminApi from '../../api/admin'
import AdminOverviewPage from './AdminOverviewPage'

describe('AdminOverviewPage', () => {
  beforeEach(() => {
    vi.spyOn(adminApi, 'getOperationsOverview').mockResolvedValue({
      activeStudents: 18,
      activeInstructors: 4,
      runningCourses: 3,
      pendingApplications: 6,
      activeEnrollments: 20,
      revenue: [{ currency: 'EGP', amount: 12500, paymentCount: 7 }],
      applicationsByStatus: [{ status: 'Pending', count: 6 }],
      paymentsByStatus: [{ status: 'Succeeded', count: 7 }],
      enrollmentsByStatus: [{ status: 'Active', count: 20 }],
      recentActivity: [{
        id: 'activity-1',
        activityType: 'Enrollment',
        status: 'Active',
        occurredAtUtc: '2026-11-02T10:00:00Z',
        learnerName: 'Demo Learner',
        offeringTitle: 'Cybersecurity Foundations',
      }],
    })
  })

  it('shows operational KPIs, reports and recent activity', async () => {
    render(<AdminOverviewPage />)

    expect(await screen.findByRole('heading', { name: 'Operations overview' })).toBeInTheDocument()
    expect(screen.getByText('12,500 EGP')).toBeInTheDocument()
    expect(screen.getByText('Demo Learner')).toBeInTheDocument()
    expect(screen.getByText('Cybersecurity Foundations')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Phase 1 reports' })).toBeInTheDocument()
  })
})
