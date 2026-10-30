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
const ACCOUNT_DRAWER_ID = 'account-drawer'

function Header() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const tokens = useAuthSession()
  const { profile } = useAuthProfile()
  const canReviewApplications = profile?.roles.includes('Manager') || profile?.roles.includes('Admin')
  const isInstructor = profile?.roles.includes('Instructor') ?? false
  const canOpenCms = profile?.roles.some((role) => (
    role === 'ContentManager' || role === 'Manager' || role === 'Admin'
  ))
  const [isNavOpen, setIsNavOpen] = useState(false)
  const [isAccountOpen, setIsAccountOpen] = useState(false)
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const navToggleRef = useRef<HTMLButtonElement>(null)
  const accountToggleRef = useRef<HTMLButtonElement>(null)
  const accountCloseRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!isNavOpen && !isAccountOpen) {
      return
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        if (isAccountOpen) {
          setIsAccountOpen(false)
          accountToggleRef.current?.focus()
        } else {
          setIsNavOpen(false)
          navToggleRef.current?.focus()
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isAccountOpen, isNavOpen])

  useEffect(() => {
    if (!isAccountOpen) {
      return
    }

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    accountCloseRef.current?.focus()

    return () => {
      document.body.style.overflow = previousOverflow
    }
  }, [isAccountOpen])

  const navClassName = isNavOpen ? 'site-nav site-nav--open' : 'site-nav'

  function closeAccountDrawer({ restoreFocus = false } = {}) {
    setIsAccountOpen(false)
    if (restoreFocus) {
      accountToggleRef.current?.focus()
    }
  }

  function handleAccountNavigation() {
    closeAccountDrawer()
    setIsNavOpen(false)
  }

  async function handleLogout() {
    setIsLoggingOut(true)

    try {
      await logout()
    } catch {
      // The local session is cleared even when the API is temporarily unavailable.
    } finally {
      setIsLoggingOut(false)
      setIsNavOpen(false)
      setIsAccountOpen(false)
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
              {!tokens ? (
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
              ) : null}
            </ul>
          </nav>

          <LanguageSwitcher />

          <div className={`site-header__auth${tokens ? ' site-header__auth--session' : ''}`}>
            {tokens ? (
              <button
                ref={accountToggleRef}
                className="account-toggle"
                type="button"
                aria-expanded={isAccountOpen}
                aria-controls={ACCOUNT_DRAWER_ID}
                onClick={() => {
                  setIsNavOpen(false)
                  setIsAccountOpen((open) => !open)
                }}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
                  <circle cx="12" cy="8" r="4" />
                  <path d="M4.5 20c.7-4 3.2-6 7.5-6s6.8 2 7.5 6" strokeLinecap="round" />
                </svg>
                <span>{t('nav.account')}</span>
              </button>
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
            onClick={() => {
              setIsAccountOpen(false)
              setIsNavOpen((open) => !open)
            }}
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

      {tokens && isAccountOpen ? (
        <div className="account-drawer-layer">
          <button
            type="button"
            className="account-drawer__backdrop"
            aria-label={t('nav.closeAccount')}
            onClick={() => closeAccountDrawer({ restoreFocus: true })}
          />
          <aside
            id={ACCOUNT_DRAWER_ID}
            className="account-drawer"
            role="dialog"
            aria-modal="true"
            aria-labelledby="account-drawer-title"
          >
            <div className="account-drawer__header">
              <div>
                <p className="account-drawer__eyebrow">{t('nav.account')}</p>
                <h2 id="account-drawer-title" dir="auto">
                  {profile ? `${profile.firstName} ${profile.lastName}` : t('app.name')}
                </h2>
                {profile ? <p className="account-drawer__email" dir="ltr">{profile.email}</p> : null}
              </div>
              <button
                type="button"
                ref={accountCloseRef}
                className="account-drawer__close"
                aria-label={t('nav.closeAccount')}
                onClick={() => closeAccountDrawer({ restoreFocus: true })}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
                </svg>
              </button>
            </div>

            <nav className="account-drawer__nav" aria-label={t('nav.accountNavigation')}>
              <NavLink to="/profile" onClick={handleAccountNavigation}>{t('nav.profile')}</NavLink>
              <NavLink to="/profile/notifications" onClick={handleAccountNavigation}>{t('nav.notifications')}</NavLink>
              {canOpenCms ? (
                <NavLink to="/cms" onClick={handleAccountNavigation}>{t('nav.cmsDashboard')}</NavLink>
              ) : null}
              {canReviewApplications ? (
                <NavLink to="/staff/applications" onClick={handleAccountNavigation}>{t('nav.reviewApplications')}</NavLink>
              ) : null}
              {canReviewApplications ? (
                <NavLink to="/admin/catalog" onClick={handleAccountNavigation}>{t('nav.adminDashboard')}</NavLink>
              ) : null}
              {isInstructor ? (
                <NavLink to="/instructor" onClick={handleAccountNavigation}>{t('nav.instructorDashboard')}</NavLink>
              ) : null}
            </nav>

            <button
              className="account-drawer__logout"
              type="button"
              disabled={isLoggingOut}
              onClick={handleLogout}
            >
              {isLoggingOut ? t('auth.actions.signingOut') : t('nav.logout')}
            </button>
          </aside>
        </div>
      ) : null}
    </header>
  )
}

export default Header
