import { useSyncExternalStore } from 'react'

export interface AuthTokens {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
}

const storageKey = 'andalusia-academy-auth-session'
const listeners = new Set<() => void>()

function readStoredTokens(): AuthTokens | null {
  try {
    const storedValue = sessionStorage.getItem(storageKey)
    if (!storedValue) {
      return null
    }

    const parsedValue = JSON.parse(storedValue) as Partial<AuthTokens>
    if (
      typeof parsedValue.accessToken !== 'string' ||
      typeof parsedValue.accessTokenExpiresAtUtc !== 'string' ||
      typeof parsedValue.refreshToken !== 'string' ||
      typeof parsedValue.refreshTokenExpiresAtUtc !== 'string'
    ) {
      sessionStorage.removeItem(storageKey)
      return null
    }

    if (Date.parse(parsedValue.refreshTokenExpiresAtUtc) <= Date.now()) {
      sessionStorage.removeItem(storageKey)
      return null
    }

    return parsedValue as AuthTokens
  } catch {
    sessionStorage.removeItem(storageKey)
    return null
  }
}

let currentTokens = readStoredTokens()

function notifyListeners() {
  listeners.forEach((listener) => listener())
}

export const authSession = {
  getTokens: () => currentTokens,
  setTokens(tokens: AuthTokens) {
    currentTokens = tokens
    sessionStorage.setItem(storageKey, JSON.stringify(tokens))
    notifyListeners()
  },
  clear() {
    currentTokens = null
    sessionStorage.removeItem(storageKey)
    notifyListeners()
  },
  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
}

export function useAuthSession() {
  return useSyncExternalStore(authSession.subscribe, authSession.getTokens, authSession.getTokens)
}
