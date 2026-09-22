import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router-dom'
import Container from './Container'
import LanguageSwitcher from './LanguageSwitcher'
import Logo from './Logo'
import { primaryNavItems } from '../navigation'

const NAV_ID = 'primary-navigation'

function Header() {
  const { t } = useTranslation()
  const [isNavOpen, setIsNavOpen] = useState(false)
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
            </ul>
          </nav>

          <LanguageSwitcher />

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
