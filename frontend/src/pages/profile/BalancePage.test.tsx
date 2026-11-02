import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as compensationApi from '../../api/compensation'
import BalancePage from './BalancePage'

afterEach(() => vi.restoreAllMocks())

describe('BalancePage', () => {
  it('shows only the signed-in account balance and supports a demo credit', async () => {
    vi.spyOn(compensationApi, 'getMyBalance').mockResolvedValue({
      balance: 500,
      currency: 'EGP',
      updatedAtUtc: '2026-11-02T10:00:00Z',
    })
    vi.spyOn(compensationApi, 'addDemoBalanceCredit').mockResolvedValue({
      balance: 700,
      currency: 'EGP',
      updatedAtUtc: '2026-11-02T10:05:00Z',
    })

    render(<BalancePage />)

    expect(await screen.findByText(/500\.00/)).toBeInTheDocument()
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Amount' }), { target: { value: '200' } })
    fireEvent.click(screen.getByRole('radio', { name: 'Fawry' }))
    fireEvent.click(screen.getByRole('button', { name: 'Add demo funds' }))

    expect(await screen.findByText(/700\.00/)).toBeInTheDocument()
    expect(compensationApi.addDemoBalanceCredit).toHaveBeenCalledWith(200, 'Fawry')
  })
})
