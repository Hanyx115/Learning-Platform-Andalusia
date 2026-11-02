import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import Container from '../../components/Container'
import Logo from '../../components/Logo'
import './auth.css'

interface AuthPageShellProps {
  eyebrow: string
  title: string
  description: string
  children: ReactNode
}

function AuthPageShell({ eyebrow, title, description, children }: AuthPageShellProps) {
  const { t } = useTranslation()

  return (
    <div className="auth-page">
      <Container className="auth-page__container">
        <aside className="auth-page__brand" aria-label={t('app.name')}>
          <div className="auth-page__brand-logo"><Logo /></div>
          <div className="auth-page__brand-copy">
            <p className="auth-page__brand-eyebrow">{t('home.hero.eyebrow')}</p>
            <h2>{t('home.hero.title')}</h2>
            <p>{t('home.hero.description')}</p>
          </div>
          <div className="auth-page__brand-tags" aria-hidden="true">
            <span>{t('nav.courses')}</span>
            <span>{t('nav.programs')}</span>
            <span>{t('nav.careerPaths')}</span>
          </div>
        </aside>
        <div className="auth-page__form-panel">
          <section className="auth-card">
            <p className="auth-card__eyebrow">{eyebrow}</p>
            <h1>{title}</h1>
            <p className="auth-card__lead">{description}</p>
            {children}
          </section>
        </div>
      </Container>
    </div>
  )
}

export default AuthPageShell
