import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import axe from 'axe-core'
import * as enrollmentApi from '../../api/enrollment'
import type { Application, PayForApplicationResult } from '../../api/enrollment'
import PaymentCheckoutPage from './PaymentCheckoutPage'

const application: Application = {
  id: 'application-1',
  userId: 'learner-1',
  courseOfferingId: 'offering-1',
  courseId: 'course-1',
  courseTitle: 'Cybersecurity Foundations',
  programOfferingId: null,
  programId: null,
  programTitle: null,
  startsAtUtc: '2026-11-01T08:00:00Z',
  endsAtUtc: '2026-12-01T08:00:00Z',
  price: 250,
  currency: 'EGP',
  status: 'Accepted',
  submittedAt: '2026-10-20T08:00:00Z',
  reviewedAt: '2026-10-21T08:00:00Z',
  rejectionReason: null,
  statusHistory: [],
}

function renderCheckout() {
  return render(
    <MemoryRouter initialEntries={['/profile/applications/application-1/payment']}>
      <Routes>
        <Route path="/profile/applications/:applicationId/payment" element={<PaymentCheckoutPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('PaymentCheckoutPage', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'getMyApplications').mockResolvedValue([application])
    vi.spyOn(enrollmentApi, 'getMyPayments').mockResolvedValue([])
  })

  it('offers card and Fawry demo methods and completes payment through the existing API', async () => {
    const result: PayForApplicationResult = {
      succeeded: true,
      payment: {
        id: '12345678-0000-0000-0000-000000000000',
        applicationId: application.id,
        amount: 250,
        currency: 'EGP',
        status: 'Succeeded',
        createdAt: '2026-10-29T09:00:00Z',
        completedAt: '2026-10-29T09:00:01Z',
      },
      enrollment: null,
      error: null,
    }
    const pay = vi.spyOn(enrollmentApi, 'payForApplication').mockResolvedValue(result)
    const user = userEvent.setup()

    renderCheckout()

    expect(await screen.findByRole('heading', { name: 'Choose how to pay' })).toBeInTheDocument()
    expect(screen.getByDisplayValue('4111 1111 1111 1111')).toBeInTheDocument()

    await user.click(screen.getByRole('radio', { name: /Fawry/i }))
    expect(screen.getByLabelText('Mobile number')).toHaveValue('01000000000')
    await user.click(screen.getByRole('button', { name: 'Pay 250 EGP' }))

    expect(pay).toHaveBeenCalledWith('application-1')
    expect(await screen.findByRole('heading', { name: 'Payment successful' })).toBeInTheDocument()
    expect(screen.getByText(/FWY-12345678/)).toBeInTheDocument()
  })

  it('has no detectable semantic accessibility violations', async () => {
    const { container } = renderCheckout()

    expect(await screen.findByRole('heading', { name: 'Choose how to pay' })).toBeInTheDocument()
    const results = await axe.run(container, {
      rules: { 'color-contrast': { enabled: false } },
    })

    expect(results.violations.map((violation) => violation.id)).toEqual([])
  })
})
