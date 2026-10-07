import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import Container from '../../components/Container'

interface CatalogHeroProps {
  eyebrow: string
  title: string
  description: string
}

export function CatalogHero({ eyebrow, title, description }: CatalogHeroProps) {
  return (
    <section className="public-page__hero">
      <Container>
        <p className="public-page__eyebrow">{eyebrow}</p>
        <h1 dir="auto">{title}</h1>
        <p className="public-page__lead" dir="auto">{description}</p>
      </Container>
    </section>
  )
}

interface QueryStateProps {
  kind: 'loading' | 'error' | 'empty'
  onRetry?: () => void
}

export function QueryState({ kind, onRetry }: QueryStateProps) {
  const { t } = useTranslation()

  return (
    <div className={`catalog-state catalog-state--${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
      <p>{t(`common.${kind}`)}</p>
      {kind === 'error' && onRetry ? (
        <button className="button button--secondary" type="button" onClick={onRetry}>
          {t('common.retry')}
        </button>
      ) : null}
    </div>
  )
}

interface CatalogMetaProps {
  children: ReactNode
}

export function CatalogMeta({ children }: CatalogMetaProps) {
  return <span className="catalog-meta" dir="auto">{children}</span>
}
