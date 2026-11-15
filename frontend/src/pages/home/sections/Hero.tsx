import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Container from '../../../components/Container'
import './Hero.css'
import { useLocalizedCmsItem } from '../CmsHomepageContent'

function Hero() {
  const { t } = useTranslation()
  const managed = useLocalizedCmsItem('HomepageSection', 'hero')

  return (
    <section className="hero">
      <Container className="hero__inner">
        <p className="hero__eyebrow">{t('home.hero.eyebrow')}</p>
        <h1 className="hero__title">{managed?.title ?? t('home.hero.title')}</h1>
        <p className="hero__description">{managed?.body ?? t('home.hero.description')}</p>
        <div className="hero__actions">
          <Link to="/courses" className="button button--primary">
            {t('home.hero.primaryCta')}
          </Link>
          <Link to="/career-paths" className="button button--secondary">
            {t('home.hero.secondaryCta')}
          </Link>
        </div>
      </Container>
    </section>
  )
}

export default Hero
