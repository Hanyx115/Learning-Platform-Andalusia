import { act } from 'react-dom/test-utils'
import { render, screen } from '@testing-library/react'
import { RouterProvider } from 'react-router-dom'
import { router } from './router'

describe('router', () => {
  it('renders the home page by default, navigates to a real route, and falls back to the 404 page', async () => {
    render(<RouterProvider router={router} />)
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
