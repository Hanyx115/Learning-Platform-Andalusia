import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router-dom'
import LanguageSwitcher from '../components/LanguageSwitcher'
import { persistLanguage, resolveLanguage } from '../i18n'

function RootLayout() {
  const { i18n, t } = useTranslation()
  const currentLanguage = resolveLanguage(i18n.resolvedLanguage ?? i18n.language)

  useEffect(() => {
    const root = document.documentElement
    root.lang = currentLanguage
    root.dir = currentLanguage === 'ar' ? 'rtl' : 'ltr'
    document.title = t('app.name')
    persistLanguage(currentLanguage)
  }, [currentLanguage, t])

  return (
    <div className="app-shell">
      <header className="foundation-header">
        <strong>{t('app.name')}</strong>
        <LanguageSwitcher />
      </header>
      <Outlet />
    </div>
  )
}

export default RootLayout
