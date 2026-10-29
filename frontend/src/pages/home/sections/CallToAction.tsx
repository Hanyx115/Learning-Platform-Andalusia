import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Container from '../../../components/Container'
import './CallToAction.css'
import { useLocalizedCmsItem } from '../CmsHomepageContent'

function CallToAction() {
  const { t } = useTranslation()
  const managed = useLocalizedCmsItem('HomepageSection', 'cta')

  return (
    <section className="cta-banner">
      <Container className="cta-banner__inner">
        <h2 className="cta-banner__title">{managed?.title ?? t('home.cta.title')}</h2>
        <p className="cta-banner__description">{managed?.body ?? t('home.cta.description')}</p>
        <div className="cta-banner__actions">
          <Link to="/register" className="button button--primary">
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
