import { createContext, useContext } from 'react'
import type { Profile } from '../api/auth'

export interface AuthProfileContextValue {
  profile: Profile | null
  isLoading: boolean
}

export const AuthProfileContext = createContext<AuthProfileContextValue>({ profile: null, isLoading: false })

export function useAuthProfile() {
  return useContext(AuthProfileContext)
}
