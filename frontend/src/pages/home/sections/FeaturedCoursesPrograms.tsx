import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Card from '../../../components/Card'
import Section from '../../../components/Section'

interface FeaturedItem {
  kind: string
  title: string
  meta: string
}

function FeaturedCoursesPrograms() {
  const { t } = useTranslation()
  const items = t('home.featured.items', { returnObjects: true }) as FeaturedItem[]

  return (
    <Section
      className="featured"
      eyebrowKey="home.featured.eyebrow"
      titleKey="home.featured.title"
      descriptionKey="home.featured.description"
    >
      <div className="card-grid">
        {items.map((item) => (
          <Card key={item.title}>
            <span className="badge">{item.kind}</span>
            <h3>{item.title}</h3>
            <p>{item.meta}</p>
          </Card>
        ))}
      </div>
      <Link to="/courses" className="section-link">
        {t('home.featured.viewAll')}
      </Link>
    </Section>
  )
}

export default FeaturedCoursesPrograms
