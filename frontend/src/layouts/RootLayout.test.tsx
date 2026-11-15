import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import RootLayout from './RootLayout'

function renderLayout(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route element={<RootLayout />}>
          <Route path="*" element={<h1>Route content</h1>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

describe('RootLayout presentation modes', () => {
  it('keeps the public footer on marketing pages', () => {
    renderLayout('/about')

    expect(screen.getByRole('contentinfo')).toBeInTheDocument()
    expect(document.querySelector('.app-shell')).not.toHaveClass('app-shell--workspace')
  })

  it('uses the focused auth shell without the public footer', () => {
    renderLayout('/login')

    expect(document.querySelector('.app-shell')).toHaveClass('app-shell--auth')
    expect(screen.queryByRole('contentinfo')).not.toBeInTheDocument()
  })

  it('uses the workspace shell without the public footer', () => {
    renderLayout('/admin/catalog')

    expect(document.querySelector('.app-shell')).toHaveClass('app-shell--workspace')
    expect(screen.queryByRole('contentinfo')).not.toBeInTheDocument()
  })
})
