import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import Container from './Container'
import './Section.css'

interface SectionProps {
  id?: string
  eyebrowKey?: string
  titleKey: string
  descriptionKey?: string
  className?: string
  children?: ReactNode
}

function Section({ id, eyebrowKey, titleKey, descriptionKey, className, children }: SectionProps) {
  const { t } = useTranslation()
  const sectionClassName = className ? `home-section ${className}` : 'home-section'

  return (
    <section id={id} className={sectionClassName}>
      <Container>
        <header className="home-section__header">
          {eyebrowKey ? <p className="home-section__eyebrow">{t(eyebrowKey)}</p> : null}
          <h2 className="home-section__title">{t(titleKey)}</h2>
          {descriptionKey ? <p className="home-section__description">{t(descriptionKey)}</p> : null}
        </header>
        {children}
      </Container>
    </section>
  )
}

export default Section
