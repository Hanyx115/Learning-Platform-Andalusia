import axe from 'axe-core'
import type { ReactNode } from 'react'
import { render, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProfileContext } from '../auth/profileContext'
import i18n from '../i18n'
import RootLayout from '../layouts/RootLayout'
import AdminLayout from '../pages/admin/AdminLayout'
import CmsLayout from '../pages/cms/CmsLayout'
import InstructorLayout from '../pages/instructor/InstructorLayout'
import ProfileLayout from '../pages/profile/ProfileLayout'

const axeOptions = {
  rules: {
    // jsdom does not calculate visual contrast; browser-based audits cover this rule.
    'color-contrast': { enabled: false },
  },
}

async function expectNoAccessibilityViolations(container: HTMLElement) {
  const results = await axe.run(container, axeOptions)
  expect(results.violations.map((violation) => violation.id)).toEqual([])
}

function RoleContent() {
  return <h1>Dashboard content</h1>
}

function renderRoleLayout(element: ReactNode, initialEntry: string) {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route path="*" element={<main>{element}</main>}>
          <Route path="*" element={<RoleContent />} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

afterEach(async () => {
  await i18n.changeLanguage('en')
  document.documentElement.lang = 'en'
  document.documentElement.dir = 'ltr'
})

describe('role layout accessibility and direction', () => {
  it.each([
    ['administrator', <AdminLayout />, '/admin/catalog'],
    ['instructor', <InstructorLayout />, '/instructor/sessions'],
    ['content manager', <CmsLayout />, '/cms/content'],
  ])('has no detectable semantic violations for the %s navigation', async (_role, layout, route) => {
    const { container } = renderRoleLayout(layout, route)
    await expectNoAccessibilityViolations(container)
  })

  it('audits learner navigation with learner-only links', async () => {
    const { container } = render(
      <AuthProfileContext.Provider value={{
        isLoading: false,
        profile: {
          id: 'learner-1',
          email: 'learner@example.com',
          firstName: 'Demo',
          lastName: 'Learner',
          phoneNumber: '+201000000000',
          countryCode: 'EG',
          preferredLanguage: 'en',
          roles: ['Learner'],
        },
      }}>
        <MemoryRouter initialEntries={['/profile/applications']}>
          <Routes>
            <Route path="/profile/*" element={<ProfileLayout />}>
              <Route path="*" element={<RoleContent />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </AuthProfileContext.Provider>,
    )

    await expectNoAccessibilityViolations(container)
  })

  it('sets Arabic RTL and English LTR on the application root', async () => {
    await i18n.changeLanguage('ar')
    const { unmount } = render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<RootLayout />}>
            <Route index element={<h1>الصفحة الرئيسية</h1>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(document.documentElement).toHaveAttribute('lang', 'ar')
      expect(document.documentElement).toHaveAttribute('dir', 'rtl')
    })
    unmount()

    await i18n.changeLanguage('en')
    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<RootLayout />}>
            <Route index element={<h1>Home</h1>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(document.documentElement).toHaveAttribute('lang', 'en')
      expect(document.documentElement).toHaveAttribute('dir', 'ltr')
    })
  })
})
