import { useSyncExternalStore } from 'react'

export interface AuthTokens {
  accessToken: string
  accessTokenExpiresAtUtc: string
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
      typeof parsedValue.refreshTokenExpiresAtUtc !== 'string'
    ) {
      sessionStorage.removeItem(storageKey)
      return null
    }

    if (Date.parse(parsedValue.refreshTokenExpiresAtUtc) <= Date.now()) {
      sessionStorage.removeItem(storageKey)
      return null
    }

    const tokens: AuthTokens = {
      accessToken: parsedValue.accessToken,
      accessTokenExpiresAtUtc: parsedValue.accessTokenExpiresAtUtc,
      refreshTokenExpiresAtUtc: parsedValue.refreshTokenExpiresAtUtc,
    }

    // Re-save only the public session fields so legacy browser sessions cannot retain a refresh token.
    sessionStorage.setItem(storageKey, JSON.stringify(tokens))
    return tokens
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
