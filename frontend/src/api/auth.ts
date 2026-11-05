import { apiRequest, ApiError, refreshAuthSession } from './client'
import { authSession, type AuthTokens } from '../auth/session'

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  phoneNumber: string
  countryCode: string
  preferredLanguage: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface ForgotPasswordResponse {
  message: string
  resetToken: string | null
}

export interface Profile {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string
  countryCode: string
  preferredLanguage: string
  learnerPetEnabled?: boolean
  learnerPetType?: 'cat' | 'owl' | 'fox'
  roles: string[]
}

export interface UpdateProfileRequest {
  firstName: string
  lastName: string
  phoneNumber: string
  countryCode: string
  preferredLanguage: string
  learnerPetEnabled: boolean
  learnerPetType: 'cat' | 'owl' | 'fox'
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export async function register(request: RegisterRequest) {
  const tokens = await apiRequest<AuthTokens>('/api/v1/auth/register', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  }, {
    includeAccessToken: false,
    retryAfterRefresh: false,
  })

  authSession.setTokens(tokens)
  return tokens
}

export async function login(request: LoginRequest) {
  const tokens = await apiRequest<AuthTokens>('/api/v1/auth/login', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  }, {
    includeAccessToken: false,
    retryAfterRefresh: false,
  })

  authSession.setTokens(tokens)
  return tokens
}

export async function logout() {
  try {
    let tokens = authSession.getTokens()

    if (tokens && Date.parse(tokens.accessTokenExpiresAtUtc) <= Date.now() + 30_000) {
      tokens = await refreshAuthSession()
    }

    if (tokens) {
      await apiRequest<void>('/api/v1/auth/logout', {
        method: 'POST',
      }, {
        retryAfterRefresh: false,
      })
    }
  } finally {
    authSession.clear()
  }
}

export function forgotPassword(email: string) {
  return apiRequest<ForgotPasswordResponse>('/api/v1/auth/forgot-password', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ email }),
  }, {
    includeAccessToken: false,
    retryAfterRefresh: false,
  })
}

export function resetPassword(email: string, token: string, newPassword: string) {
  return apiRequest<void>('/api/v1/auth/reset-password', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify({ email, token, newPassword }),
  }, {
    includeAccessToken: false,
    retryAfterRefresh: false,
  })
}

export function getProfile() {
  return apiRequest<Profile>('/api/v1/profile')
}

export function updateProfile(request: UpdateProfileRequest) {
  return apiRequest<Profile>('/api/v1/profile', {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function getApiErrorMessage(error: unknown, fallback: string) {
  if (!(error instanceof ApiError)) {
    return fallback
  }

  const details = error.details as ProblemDetails | string | null
  if (typeof details === 'string') {
    return details.trim() || fallback
  }

  if (details?.errors) {
    const messages = Object.values(details.errors).flat()
    if (messages.length > 0) {
      return messages.join(' ')
    }
  }

  return details?.detail || details?.title || fallback
}

export function getApiErrorStatus(error: unknown) {
  return error instanceof ApiError ? error.status : null
}
