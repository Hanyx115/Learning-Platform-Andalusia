import { act, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProfileContext } from '../auth/profileContext'
import LearnerCompanion from './LearnerCompanion'

const learnerProfile = {
  id: 'learner-1',
  email: 'learner@example.com',
  firstName: 'Demo',
  lastName: 'Learner',
  phoneNumber: '+201000000000',
  countryCode: 'EG',
  preferredLanguage: 'en',
  learnerPetEnabled: true,
  learnerPetType: 'fox' as const,
  roles: ['Learner'],
}

describe('LearnerCompanion', () => {
  afterEach(() => vi.useRealTimers())

  it('shows the selected pet and offers help after fifteen minutes of inactivity', () => {
    vi.useFakeTimers()
    render(
      <AuthProfileContext.Provider value={{ profile: learnerProfile, isLoading: false }}>
        <LearnerCompanion />
      </AuthProfileContext.Provider>,
    )

    expect(screen.getByRole('button', { name: 'Open Fox companion' })).toBeInTheDocument()
    expect(screen.queryByText(/coffee break/i)).not.toBeInTheDocument()

    act(() => vi.advanceTimersByTime(15 * 60 * 1000))

    expect(screen.getByText(/coffee break is taking too long/i)).toBeInTheDocument()
  })

  it('stays hidden when the learner has disabled it', () => {
    render(
      <AuthProfileContext.Provider
        value={{ profile: { ...learnerProfile, learnerPetEnabled: false }, isLoading: false }}
      >
        <LearnerCompanion />
      </AuthProfileContext.Provider>,
    )

    expect(screen.queryByLabelText('Learning companion')).not.toBeInTheDocument()
  })
})
