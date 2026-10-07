import { QueryClientProvider } from '@tanstack/react-query'
import { act } from 'react-dom/test-utils'
import { render, screen } from '@testing-library/react'
import { RouterProvider } from 'react-router-dom'
import { beforeEach, vi } from 'vitest'
import { createQueryClient } from './queryClient'
import { router } from './router'

describe('router', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
      const url = String(input)
      const body = url.includes('/courses')
        ? { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 }
        : []

      return Promise.resolve(new Response(JSON.stringify(body), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }))
    }))
  })

  it('renders the home page by default, navigates to a real route, and falls back to the 404 page', async () => {
    render(
      <QueryClientProvider client={createQueryClient()}>
        <RouterProvider router={router} />
      </QueryClientProvider>,
    )
    expect(
      screen.getByRole('heading', { level: 1, name: 'Develop your skills and learn something new' }),
    ).toBeInTheDocument()

    await act(async () => {
      await router.navigate('/about')
    })
    expect(screen.getByRole('heading', { name: 'Learning that supports professional growth' })).toBeInTheDocument()

    await act(async () => {
      await router.navigate('/this-route-does-not-exist')
    })
    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument()
  })
})
