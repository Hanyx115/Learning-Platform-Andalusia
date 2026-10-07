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

interface CatalogPaginationProps {
  page: number
  totalPages: number
  onPageChange: (page: number) => void
}

export function CatalogPagination({ page, totalPages, onPageChange }: CatalogPaginationProps) {
  const { t } = useTranslation()

  if (totalPages <= 1) {
    return null
  }

  return (
    <nav className="catalog-pagination" aria-label={t('common.paginationLabel')}>
      <button
        className="button button--secondary"
        type="button"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        {t('common.previous')}
      </button>
      <span>{t('common.pageSummary', { page, totalPages })}</span>
      <button
        className="button button--secondary"
        type="button"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        {t('common.next')}
      </button>
    </nav>
  )
}
