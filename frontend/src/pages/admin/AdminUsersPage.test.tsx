import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProfileContext } from '../../auth/profileContext'
import AdminUsersPage from './AdminUsersPage'

const adminProfile = {
  id: 'admin-1',
  email: 'admin@example.com',
  firstName: 'Admin',
  lastName: 'User',
  phoneNumber: '+201000000000',
  countryCode: 'EG',
  preferredLanguage: 'en',
  roles: ['Admin'],
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('AdminUsersPage', () => {
  it('keeps role controls aligned and allows Manager assignment', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([{
      id: 'manager-1',
      email: 'manager.demo@andalusia.test',
      firstName: 'Demo',
      lastName: 'Manager',
      phoneNumber: '+201000000001',
      isActive: true,
      roles: ['Manager'],
    }]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    })))

    render(
      <AuthProfileContext.Provider value={{ profile: adminProfile, isLoading: false }}>
        <AdminUsersPage />
      </AuthProfileContext.Provider>,
    )

    const manager = await screen.findByRole('checkbox', { name: 'Manager' })
    expect(manager).toBeChecked()
    expect(manager.closest('fieldset')).toHaveClass('admin-user-roles')
    expect(screen.getByRole('checkbox', { name: 'Learner' })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Instructor' })).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'Content Manager' })).toBeInTheDocument()
  })
})
