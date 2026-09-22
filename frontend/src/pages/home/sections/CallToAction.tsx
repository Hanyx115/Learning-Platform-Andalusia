import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Container from '../../../components/Container'
import './CallToAction.css'

function CallToAction() {
  const { t } = useTranslation()

  return (
    <section className="cta-banner">
      <Container className="cta-banner__inner">
        <h2 className="cta-banner__title">{t('home.cta.title')}</h2>
        <p className="cta-banner__description">{t('home.cta.description')}</p>
        <div className="cta-banner__actions">
          <Link to="/courses" className="button button--primary">
            {t('home.cta.primaryCta')}
          </Link>
          <Link to="/about" className="button button--secondary">
            {t('home.cta.secondaryCta')}
          </Link>
        </div>
      </Container>
    </section>
  )
}

export default CallToAction
