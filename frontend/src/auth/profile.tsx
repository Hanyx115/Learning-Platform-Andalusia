import { useEffect, useState, type ReactNode } from 'react'
import { getProfile, type Profile } from '../api/auth'
import { AuthProfileContext } from './profileContext'
import { useAuthSession } from './session'

export function AuthProfileProvider({ children }: { children: ReactNode }) {
  const tokens = useAuthSession()
  const [loadedProfile, setLoadedProfile] = useState<Profile | null>(null)

  useEffect(() => {
    if (!tokens) {
      return
    }

    let isCurrent = true

    getProfile()
      .then((profile) => {
        if (isCurrent) {
          setLoadedProfile(profile)
        }
      })
      .catch(() => {
        if (isCurrent) {
          setLoadedProfile(null)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [tokens])

  // Derived at render time so signing out clears the profile immediately, without waiting on an
  // extra effect-driven render.
  const value = { profile: tokens ? loadedProfile : null }

  return (
    <AuthProfileContext.Provider value={value}>
      {children}
    </AuthProfileContext.Provider>
  )
}
