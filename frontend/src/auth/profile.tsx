import { useEffect, useState, type ReactNode } from 'react'
import { getProfile, type Profile } from '../api/auth'
import { AuthProfileContext } from './profileContext'
import { useAuthSession } from './session'

export function AuthProfileProvider({ children }: { children: ReactNode }) {
  const tokens = useAuthSession()
  const [loaded, setLoaded] = useState<{ accessToken: string; profile: Profile | null } | null>(null)

  useEffect(() => {
    if (!tokens) {
      return
    }

    let isCurrent = true
    const accessToken = tokens.accessToken

    getProfile()
      .then((profile) => {
        if (isCurrent) {
          setLoaded({ accessToken, profile })
        }
      })
      .catch(() => {
        if (isCurrent) {
          setLoaded({ accessToken, profile: null })
        }
      })

    return () => {
      isCurrent = false
    }
  }, [tokens])

  // Derived at render time so signing out clears the profile immediately, without waiting on an
  // extra effect-driven render.
  const hasLoadedCurrentSession = Boolean(tokens && loaded?.accessToken === tokens.accessToken)
  const value = {
    profile: hasLoadedCurrentSession ? loaded?.profile ?? null : null,
    isLoading: Boolean(tokens) && !hasLoadedCurrentSession,
  }

  return (
    <AuthProfileContext.Provider value={value}>
      {children}
    </AuthProfileContext.Provider>
  )
}
