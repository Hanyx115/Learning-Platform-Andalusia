import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Card from '../../../components/Card'
import Section from '../../../components/Section'

interface CareerPathItem {
  title: string
  description: string
}

function CareerPathDiscovery() {
  const { t } = useTranslation()
  const items = t('home.careerPaths.items', { returnObjects: true }) as CareerPathItem[]

  return (
    <Section
      className="career-paths"
      eyebrowKey="home.careerPaths.eyebrow"
      titleKey="home.careerPaths.title"
      descriptionKey="home.careerPaths.description"
    >
      <div className="card-grid">
        {items.map((item) => (
          <Card key={item.title}>
            <h3>{item.title}</h3>
            <p>{item.description}</p>
          </Card>
        ))}
      </div>
      <Link to="/career-paths" className="section-link">
        {t('home.careerPaths.viewAll')}
      </Link>
    </Section>
  )
}

export default CareerPathDiscovery
