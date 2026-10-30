import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation } from 'react-router-dom'
import { AuthProfileProvider } from '../auth/profile'
import Footer from '../components/Footer'
import Header from '../components/Header'
import { persistLanguage, resolveLanguage } from '../i18n'

function RootLayout() {
  const { i18n, t } = useTranslation()
  const { pathname } = useLocation()
  const currentLanguage = resolveLanguage(i18n.resolvedLanguage ?? i18n.language)
  const isWorkspace = ['/profile', '/admin', '/cms', '/instructor', '/staff']
    .some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`))
  const isAuthPage = ['/login', '/register', '/forgot-password', '/reset-password']
    .some((path) => pathname === path)
  const shellClassName = [
    'app-shell',
    isWorkspace ? 'app-shell--workspace' : '',
    isAuthPage ? 'app-shell--auth' : '',
  ].filter(Boolean).join(' ')

  useEffect(() => {
    const root = document.documentElement
    root.lang = currentLanguage
    root.dir = currentLanguage === 'ar' ? 'rtl' : 'ltr'
    document.title = t('app.name')
    persistLanguage(currentLanguage)
  }, [currentLanguage, t])

  return (
    <AuthProfileProvider>
      <div className={shellClassName}>
        <Header />
        <main>
          <Outlet />
        </main>
        {!isWorkspace && !isAuthPage ? <Footer /> : null}
      </div>
    </AuthProfileProvider>
  )
}

export default RootLayout
