import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, useNavigate } from 'react-router-dom'
import { logout } from '../api/auth'
import { useAuthSession } from '../auth/session'
import { useAuthProfile } from '../auth/profileContext'
import Container from './Container'
import './Header.css'
import LanguageSwitcher from './LanguageSwitcher'
import Logo from './Logo'
import { primaryNavItems } from '../navigation'

const NAV_ID = 'primary-navigation'

function Header() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const tokens = useAuthSession()
  const { profile } = useAuthProfile()
  const canReviewApplications = profile?.roles.includes('Manager') || profile?.roles.includes('Admin')
  const isInstructor = profile?.roles.includes('Instructor') ?? false
  const [isNavOpen, setIsNavOpen] = useState(false)
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const navToggleRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!isNavOpen) {
      return
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsNavOpen(false)
        navToggleRef.current?.focus()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isNavOpen])

  const navClassName = isNavOpen ? 'site-nav site-nav--open' : 'site-nav'

  async function handleLogout() {
    setIsLoggingOut(true)

    try {
      await logout()
    } catch {
      // The local session is cleared even when the API is temporarily unavailable.
    } finally {
      setIsLoggingOut(false)
      setIsNavOpen(false)
      navigate('/login', { replace: true })
    }
  }

  return (
    <header className="site-header">
      <Container className="site-header__bar">
        <NavLink to="/" className="site-logo" aria-label={t('app.name')} onClick={() => setIsNavOpen(false)}>
          <Logo />
        </NavLink>

        <div className="site-header__actions">
          <nav id={NAV_ID} className={navClassName} aria-label={t('nav.label')}>
            <ul className="site-nav__list">
              {primaryNavItems.map((item) => (
                <li key={item.path}>
                  <NavLink
                    to={item.path}
                    end={item.path === '/'}
                    className="site-nav__link"
                    onClick={() => setIsNavOpen(false)}
                  >
                    {t(item.labelKey)}
                  </NavLink>
                </li>
              ))}
              {tokens ? (
                <>
                  <li className="site-nav__auth-item">
                    <NavLink className="site-nav__link" to="/profile" onClick={() => setIsNavOpen(false)}>
                      {t('nav.profile')}
                    </NavLink>
                  </li>
                  {canReviewApplications ? (
                    <li className="site-nav__auth-item">
                      <NavLink className="site-nav__link" to="/staff/applications" onClick={() => setIsNavOpen(false)}>
                        {t('nav.reviewApplications')}
                      </NavLink>
                    </li>
                  ) : null}
                  {isInstructor ? (
                    <li className="site-nav__auth-item">
                      <NavLink className="site-nav__link" to="/instructor" onClick={() => setIsNavOpen(false)}>
                        {t('nav.instructorDashboard')}
                      </NavLink>
                    </li>
                  ) : null}
                  <li className="site-nav__auth-item">
                    <button
                      className="site-nav__link site-nav__button"
                      type="button"
                      disabled={isLoggingOut}
                      onClick={handleLogout}
                    >
                      {isLoggingOut ? t('auth.actions.signingOut') : t('nav.logout')}
                    </button>
                  </li>
                </>
              ) : (
                <>
                  <li className="site-nav__auth-item">
                    <NavLink className="site-nav__link" to="/login" onClick={() => setIsNavOpen(false)}>
                      {t('nav.login')}
                    </NavLink>
                  </li>
                  <li className="site-nav__auth-item">
                    <NavLink className="site-nav__link" to="/register" onClick={() => setIsNavOpen(false)}>
                      {t('nav.register')}
                    </NavLink>
                  </li>
                </>
              )}
            </ul>
          </nav>

          <LanguageSwitcher />

          <div className="site-header__auth">
            {tokens ? (
              <>
                <NavLink className="site-header__auth-link" to="/profile" onClick={() => setIsNavOpen(false)}>
                  {t('nav.profile')}
                </NavLink>
                {canReviewApplications ? (
                  <NavLink className="site-header__auth-link" to="/staff/applications" onClick={() => setIsNavOpen(false)}>
                    {t('nav.reviewApplications')}
                  </NavLink>
                ) : null}
                {isInstructor ? (
                  <NavLink className="site-header__auth-link" to="/instructor" onClick={() => setIsNavOpen(false)}>
                    {t('nav.instructorDashboard')}
                  </NavLink>
                ) : null}
                <button
                  className="site-header__logout"
                  type="button"
                  disabled={isLoggingOut}
                  onClick={handleLogout}
                >
                  {isLoggingOut ? t('auth.actions.signingOut') : t('nav.logout')}
                </button>
              </>
            ) : (
              <>
                <NavLink className="site-header__auth-link" to="/login" onClick={() => setIsNavOpen(false)}>
                  {t('nav.login')}
                </NavLink>
                <NavLink
                  className="site-header__auth-link site-header__auth-link--primary"
                  to="/register"
                  onClick={() => setIsNavOpen(false)}
                >
                  {t('nav.register')}
                </NavLink>
              </>
            )}
          </div>

          <button
            type="button"
            ref={navToggleRef}
            className="nav-toggle"
            aria-expanded={isNavOpen}
            aria-controls={NAV_ID}
            aria-label={t(isNavOpen ? 'nav.close' : 'nav.open')}
            onClick={() => setIsNavOpen((open) => !open)}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              {isNavOpen ? (
                <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
              ) : (
                <path d="M4 7h16M4 12h16M4 17h16" strokeLinecap="round" />
              )}
            </svg>
          </button>
        </div>
      </Container>
    </header>
  )
}

export default Header
