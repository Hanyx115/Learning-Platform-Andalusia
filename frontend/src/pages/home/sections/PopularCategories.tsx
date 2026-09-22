import { useTranslation } from 'react-i18next'
import Card from '../../../components/Card'
import Section from '../../../components/Section'

interface CategoryItem {
  name: string
  count: string
}

function PopularCategories() {
  const { t } = useTranslation()
  const items = t('home.categories.items', { returnObjects: true }) as CategoryItem[]

  return (
    <Section
      className="categories"
      eyebrowKey="home.categories.eyebrow"
      titleKey="home.categories.title"
      descriptionKey="home.categories.description"
    >
      <div className="card-grid card-grid--compact">
        {items.map((item) => (
          <Card key={item.name} className="category-card">
            <span className="icon-circle" aria-hidden="true">
              {item.name.charAt(0)}
            </span>
            <h3>{item.name}</h3>
            <p>{item.count}</p>
          </Card>
        ))}
      </div>
    </Section>
  )
}

export default PopularCategories
