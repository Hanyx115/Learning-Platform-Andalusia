import { useTranslation } from 'react-i18next'
import Card from '../../../components/Card'
import Section from '../../../components/Section'

interface TestimonialItem {
  quote: string
  name: string
  role: string
}

function Testimonials() {
  const { t } = useTranslation()
  const items = t('home.testimonials.items', { returnObjects: true }) as TestimonialItem[]

  return (
    <Section
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
