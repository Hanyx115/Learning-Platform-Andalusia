import { authSession, type AuthTokens } from '../auth/session'

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7115'
const apiBaseUrl = configuredBaseUrl.replace(/\/$/, '')

export function resolveApiUrl(path: string) {
  if (/^https?:\/\//i.test(path)) {
    return path
  }

  return `${apiBaseUrl}${path.startsWith('/') ? path : `/${path}`}`
}

export class ApiError extends Error {
  readonly status: number
  readonly details: unknown

  constructor(
    status: number,
    details: unknown,
  ) {
    super(`API request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.details = details
  }
}

interface ApiRequestConfiguration {
  includeAccessToken?: boolean
  retryAfterRefresh?: boolean
}

let refreshInProgress: Promise<AuthTokens | null> | null = null

async function readResponseBody(response: Response) {
  if (response.status === 204) {
    return undefined
  }

  const contentType = response.headers.get('content-type')
  return contentType?.includes('json')
    ? response.json()
    : response.text()
}

async function refreshSession(): Promise<AuthTokens | null> {
  if (refreshInProgress) {
    return refreshInProgress
  }

  const currentSession = authSession.getTokens()
  if (!currentSession || Date.parse(currentSession.refreshTokenExpiresAtUtc) <= Date.now()) {
    authSession.clear()
    return null
  }

  refreshInProgress = (async () => {
    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/auth/refresh`, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
        },
        credentials: 'include',
      })

      if (!response.ok) {
        authSession.clear()
        return null
      }

      const tokens = await response.json() as AuthTokens
      authSession.setTokens(tokens)
      return tokens
    } catch {
      return null
    } finally {
      refreshInProgress = null
    }
  })()

  return refreshInProgress
}

export function refreshAuthSession() {
  return refreshSession()
}

async function sendRequest(
  path: string,
  options: RequestInit,
  includeAccessToken: boolean,
) {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')

  const accessToken = authSession.getTokens()?.accessToken
  if (includeAccessToken && accessToken) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  return fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
    credentials: 'include',
  })
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
  configuration: ApiRequestConfiguration = {},
): Promise<T> {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const includeAccessToken = configuration.includeAccessToken ?? true
  const retryAfterRefresh = configuration.retryAfterRefresh ?? true
  let response = await sendRequest(normalizedPath, options, includeAccessToken)

  if (response.status === 401 && retryAfterRefresh && authSession.getTokens()) {
    const refreshedTokens = await refreshSession()
    if (refreshedTokens) {
      response = await sendRequest(normalizedPath, options, includeAccessToken)
    }
  }

  if (!response.ok) {
    if (response.status === 401) {
      authSession.clear()
    }

    throw new ApiError(response.status, await readResponseBody(response))
  }

  return await readResponseBody(response) as T
}

export async function apiDownload(path: string): Promise<Blob> {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  let response = await sendRequest(normalizedPath, {}, true)

  if (response.status === 401 && authSession.getTokens()) {
    const refreshedTokens = await refreshSession()
    if (refreshedTokens) {
      response = await sendRequest(normalizedPath, {}, true)
    }
  }

  if (!response.ok) {
    if (response.status === 401) {
      authSession.clear()
    }

    throw new ApiError(response.status, await readResponseBody(response))
  }

  return response.blob()
}
