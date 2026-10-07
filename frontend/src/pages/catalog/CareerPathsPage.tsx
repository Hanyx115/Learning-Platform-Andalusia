import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { useCareerPaths } from '../../api/catalogQueries'
import Card from '../../components/Card'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, CatalogPagination, QueryState } from './CatalogUi'
import './catalog.css'

const PAGE_SIZE = 6

function CareerPathsPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedPage = Number(searchParams.get('page'))
  const page = Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const query = useCareerPaths({ page, pageSize: PAGE_SIZE, status: 'Published' })
  const paths = query.data?.items ?? []

  function changePage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    if (nextPage === 1) {
      next.delete('page')
    } else {
      next.set('page', String(nextPage))
    }
    setSearchParams(next)
  }

  return (
    <>
      <CatalogHero
        eyebrow={t('careerPaths.eyebrow')}
        title={t('careerPaths.title')}
        description={t('careerPaths.description')}
      />
      <section className="catalog-page">
        <Container>
          {query.isPending ? <QueryState kind="loading" /> : null}
          {query.isError ? <QueryState kind="error" onRetry={() => void query.refetch()} /> : null}
          {query.isSuccess && paths.length === 0 ? <QueryState kind="empty" /> : null}
          {paths.length > 0 ? (
            <>
              <p className="catalog-results">{t('careerPaths.results', { count: query.data?.totalCount })}</p>
              <div className="catalog-grid">
                {paths.map((path) => (
                  <Card key={path.id} as="article" className="catalog-card">
                    <CatalogPhoto slug={path.slug} className="catalog-card__image" />
                    <CatalogMeta>{t('careerPaths.pathLabel')}</CatalogMeta>
                    <h2 dir="auto"><Link to={`/career-paths/${path.slug}`}>{path.title}</Link></h2>
                    <p dir="auto">{path.description}</p>
                    <Link className="catalog-card__link" to={`/career-paths/${path.slug}`}>{t('common.viewDetails')}</Link>
                  </Card>
                ))}
              </div>
              <CatalogPagination
                page={page}
                totalPages={query.data?.totalPages ?? 0}
                onPageChange={changePage}
              />
            </>
          ) : null}
        </Container>
      </section>
    </>
  )
}

export default CareerPathsPage
