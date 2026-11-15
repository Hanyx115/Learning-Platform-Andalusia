import { useTranslation } from 'react-i18next'
import Card from '../../../components/Card'
import Section from '../../../components/Section'

interface ComingSoonItem {
  title: string
  meta: string
}

function ComingSoon() {
  const { t } = useTranslation()
  const items = t('home.comingSoon.items', { returnObjects: true }) as ComingSoonItem[]

  return (
    <Section
      contentKey="coming-soon"
      className="coming-soon"
      eyebrowKey="home.comingSoon.eyebrow"
      titleKey="home.comingSoon.title"
      descriptionKey="home.comingSoon.description"
    >
      <div className="card-grid">
        {items.map((item) => (
          <Card key={item.title}>
            <span className="badge">{item.meta}</span>
            <h3>{item.title}</h3>
          </Card>
        ))}
      </div>
    </Section>
  )
}

export default ComingSoon
