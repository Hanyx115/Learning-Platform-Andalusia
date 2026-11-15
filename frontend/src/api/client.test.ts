import { afterEach, beforeEach, vi } from 'vitest'
import { authSession, type AuthTokens } from '../auth/session'
import { apiRequest } from './client'

const initialTokens: AuthTokens = {
  accessToken: 'old-access-token',
  accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z',
  refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z',
}

const refreshedTokens: AuthTokens = {
  accessToken: 'new-access-token',
  accessTokenExpiresAtUtc: '2099-01-01T00:15:00Z',
  refreshTokenExpiresAtUtc: '2099-02-01T00:15:00Z',
}

describe('apiRequest authentication', () => {
  beforeEach(() => {
    authSession.clear()
  })

  afterEach(() => {
    authSession.clear()
    vi.unstubAllGlobals()
  })

  it('refreshes once after a 401 and retries with the new access token', async () => {
    authSession.setTokens(initialTokens)

    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(JSON.stringify(refreshedTokens), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ firstName: 'Mazen' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }))

    vi.stubGlobal('fetch', fetchMock)

    await expect(apiRequest<{ firstName: string }>('/api/v1/profile')).resolves.toEqual({ firstName: 'Mazen' })
    expect(fetchMock).toHaveBeenCalledTimes(3)

    const firstHeaders = fetchMock.mock.calls[0][1]?.headers as Headers
    const retryHeaders = fetchMock.mock.calls[2][1]?.headers as Headers
    expect(firstHeaders.get('Authorization')).toBe('Bearer old-access-token')
    expect(retryHeaders.get('Authorization')).toBe('Bearer new-access-token')
    expect(fetchMock.mock.calls[1][1]?.credentials).toBe('include')
    expect(authSession.getTokens()).toEqual(refreshedTokens)
  })

  it('clears the browser session when refresh is rejected', async () => {
    authSession.setTokens(initialTokens)

    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 401 }))

    vi.stubGlobal('fetch', fetchMock)

    await expect(apiRequest('/api/v1/profile')).rejects.toMatchObject({ status: 401 })
    expect(authSession.getTokens()).toBeNull()
  })
})
