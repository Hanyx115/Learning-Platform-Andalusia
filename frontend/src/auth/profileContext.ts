import { createContext, useContext } from 'react'
import type { Profile } from '../api/auth'

export interface AuthProfileContextValue {
  profile: Profile | null
}

export const AuthProfileContext = createContext<AuthProfileContextValue>({ profile: null })

export function useAuthProfile() {
  return useContext(AuthProfileContext)
}
