import { useTranslation } from 'react-i18next'
import Card from '../../../components/Card'
import Section from '../../../components/Section'
import { localizeCmsItem, useCmsItems } from '../CmsHomepageContent'

interface TestimonialItem {
  quote: string
  name: string
  role: string
}

function Testimonials() {
  const { t, i18n } = useTranslation()
  const managedItems = useCmsItems('Testimonial')
  const fallbackItems = t('home.testimonials.items', { returnObjects: true }) as TestimonialItem[]
  const items = managedItems.length > 0
    ? managedItems.map((item) => {
        const localized = localizeCmsItem(item, i18n.resolvedLanguage)
        const [name, role = ''] = localized.title.split(' - ', 2)
        return { quote: localized.body, name, role }
      })
    : fallbackItems

  return (
    <Section
      contentKey="testimonials"
      className="testimonials"
      eyebrowKey="home.testimonials.eyebrow"
      titleKey="home.testimonials.title"
      descriptionKey="home.testimonials.description"
    >
      <div className="card-grid">
        {items.map((item) => (
          <Card key={item.name}>
            <p className="testimonial-card__quote">&ldquo;{item.quote}&rdquo;</p>
            <div className="testimonial-card__author">
              <span className="icon-circle" aria-hidden="true">
                {item.name.charAt(0)}
              </span>
              <div>
                <p className="testimonial-card__name">{item.name}</p>
                <p className="testimonial-card__role">{item.role}</p>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </Section>
  )
}

export default Testimonials
