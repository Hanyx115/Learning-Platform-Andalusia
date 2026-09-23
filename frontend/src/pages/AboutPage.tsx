import { useTranslation } from 'react-i18next'
import Container from '../components/Container'
import './PublicPages.css'

function AboutPage() {
  const { t } = useTranslation()
  const offerings = t('about.offerings', { returnObjects: true }) as string[]

  return (
    <div className="public-page">
      <section className="public-page__hero">
        <Container>
          <p className="public-page__eyebrow">{t('about.eyebrow')}</p>
          <h1>{t('about.title')}</h1>
          <p className="public-page__lead">{t('about.description')}</p>
        </Container>
      </section>

      <section className="public-page__section">
        <Container className="about-grid">
          <article className="public-page__panel">
            <h2>{t('about.excellenceTitle')}</h2>
            <p>{t('about.excellenceDescription')}</p>
          </article>

          <article className="public-page__panel">
            <h2>{t('about.offeringsTitle')}</h2>
            <ul className="public-page__list">
              {offerings.map((offering) => (
                <li key={offering}>{offering}</li>
              ))}
            </ul>
          </article>
        </Container>
      </section>
    </div>
  )
}

export default AboutPage
