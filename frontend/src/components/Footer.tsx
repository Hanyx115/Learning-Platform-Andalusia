import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Container from './Container'
import './Footer.css'
import Logo from './Logo'
import { primaryNavItems } from '../navigation'

function Footer() {
  const { t } = useTranslation()
  const year = new Date().getFullYear()

  return (
    <footer className="site-footer">
      <Container className="site-footer__content">
        <div>
          <Link to="/" className="site-footer__brand" aria-label={t('app.name')}>
            <Logo />
          </Link>
          <p className="site-footer__tagline">{t('footer.tagline')}</p>
        </div>

        <nav aria-label={t('footer.navLabel')}>
          <ul className="footer-nav__list">
            {primaryNavItems.map((item) => (
              <li key={item.path}>
                <Link to={item.path} className="footer-link">
                  {t(item.labelKey)}
                </Link>
              </li>
            ))}
          </ul>
        </nav>

        <div>
          <ul className="footer-nav__list">
            <li>
              <a className="footer-link" href="mailto:hello@andalusia.academy">
                {t('footer.emailLabel')}
              </a>
            </li>
          </ul>
        </div>
      </Container>

      <div className="site-footer__bottom">
        <Container>{t('footer.copyright', { year })}</Container>
      </div>
    </footer>
  )
}

export default Footer
