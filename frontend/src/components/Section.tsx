import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import Container from './Container'
import './Section.css'
import { useLocalizedCmsItem } from '../pages/home/CmsHomepageContent'

interface SectionProps {
  id?: string
  eyebrowKey?: string
  titleKey: string
  descriptionKey?: string
  className?: string
  children?: ReactNode
  contentKey?: string
}

function Section({ id, eyebrowKey, titleKey, descriptionKey, className, children, contentKey }: SectionProps) {
  const { t } = useTranslation()
  const managed = useLocalizedCmsItem('HomepageSection', contentKey ?? '')
  const sectionClassName = className ? `home-section ${className}` : 'home-section'

  return (
    <section id={id} className={sectionClassName}>
      <Container>
        <header className="home-section__header">
          {eyebrowKey ? <p className="home-section__eyebrow">{t(eyebrowKey)}</p> : null}
          <h2 className="home-section__title">{managed?.title ?? t(titleKey)}</h2>
          {descriptionKey || managed?.body ? (
            <p className="home-section__description">{managed?.body ?? t(descriptionKey!)}</p>
          ) : null}
        </header>
        {children}
      </Container>
    </section>
  )
}

export default Section
