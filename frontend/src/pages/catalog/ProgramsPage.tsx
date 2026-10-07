import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { usePrograms } from '../../api/catalogQueries'
import Card from '../../components/Card'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, CatalogPagination, QueryState } from './CatalogUi'
import './catalog.css'

const PAGE_SIZE = 6

function ProgramsPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedPage = Number(searchParams.get('page'))
  const page = Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const query = usePrograms({ page, pageSize: PAGE_SIZE, status: 'Published' })
  const programs = query.data?.items ?? []

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
      <CatalogHero eyebrow={t('programs.eyebrow')} title={t('programs.title')} description={t('programs.description')} />
      <section className="catalog-page">
        <Container>
          {query.isPending ? <QueryState kind="loading" /> : null}
          {query.isError ? <QueryState kind="error" onRetry={() => void query.refetch()} /> : null}
          {query.isSuccess && programs.length === 0 ? <QueryState kind="empty" /> : null}
          {programs.length > 0 ? (
            <>
              <p className="catalog-results">{t('programs.results', { count: query.data?.totalCount })}</p>
              <div className="catalog-grid">
                {programs.map((program) => (
                  <Card key={program.id} as="article" className="catalog-card">
                    <CatalogPhoto slug={program.slug} className="catalog-card__image" />
                    <CatalogMeta>{t('programs.programLabel')}</CatalogMeta>
                    <h2 dir="auto"><Link to={`/programs/${program.slug}`}>{program.title}</Link></h2>
                    <p dir="auto">{program.description}</p>
                    <Link className="catalog-card__link" to={`/programs/${program.slug}`}>{t('common.viewDetails')}</Link>
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

export default ProgramsPage
