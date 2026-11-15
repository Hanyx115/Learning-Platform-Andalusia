import { useTranslation } from 'react-i18next'
import Section from '../../../components/Section'
import './PartnersTrust.css'
import { localizeCmsItem, useCmsItems } from '../CmsHomepageContent'

function PartnersTrust() {
  const { t, i18n } = useTranslation()
  const managedItems = useCmsItems('Partner')
  const fallbackItems = t('home.partners.items', { returnObjects: true }) as string[]
  const items = managedItems.length > 0
    ? managedItems.map((item) => localizeCmsItem(item, i18n.resolvedLanguage).title)
    : fallbackItems

  return (
    <Section
      contentKey="partners"
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
