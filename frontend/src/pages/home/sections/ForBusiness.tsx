import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Section from '../../../components/Section'
import './ForBusiness.css'

function ForBusiness() {
  const { t } = useTranslation()
  const highlights = t('home.forBusiness.highlights', { returnObjects: true }) as string[]

  return (
    <Section
      className="for-business"
      eyebrowKey="home.forBusiness.eyebrow"
      titleKey="home.forBusiness.title"
      descriptionKey="home.forBusiness.description"
    >
      <ul className="highlight-list">
        {highlights.map((highlight) => (
          <li key={highlight}>{highlight}</li>
        ))}
      </ul>
      <Link to="/for-business" className="button button--primary for-business__cta">
        {t('home.forBusiness.cta')}
      </Link>
    </Section>
  )
}

export default ForBusiness
