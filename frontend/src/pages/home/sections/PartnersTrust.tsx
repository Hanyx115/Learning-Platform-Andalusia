import { useTranslation } from 'react-i18next'
import Section from '../../../components/Section'
import './PartnersTrust.css'

function PartnersTrust() {
  const { t } = useTranslation()
  const items = t('home.partners.items', { returnObjects: true }) as string[]

  return (
    <Section
      className="partners"
      eyebrowKey="home.partners.eyebrow"
      titleKey="home.partners.title"
      descriptionKey="home.partners.description"
    >
      <div className="partners-row">
        {items.map((name) => (
          <span key={name} className="partner-badge">
            {name}
          </span>
        ))}
      </div>
    </Section>
  )
}

export default PartnersTrust
