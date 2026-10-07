import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { usePrograms } from '../../api/catalogQueries'
import Card from '../../components/Card'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, QueryState } from './CatalogUi'
import './catalog.css'

function ProgramsPage() {
  const { t } = useTranslation()
  const query = usePrograms()
  const programs = query.data?.filter((program) => program.status === 'Published') ?? []

  return (
    <>
      <CatalogHero eyebrow={t('programs.eyebrow')} title={t('programs.title')} description={t('programs.description')} />
      <section className="catalog-page">
        <Container>
          {query.isPending ? <QueryState kind="loading" /> : null}
          {query.isError ? <QueryState kind="error" onRetry={() => void query.refetch()} /> : null}
          {query.isSuccess && programs.length === 0 ? <QueryState kind="empty" /> : null}
          {programs.length > 0 ? (
            <div className="catalog-grid">
              {programs.map((program) => (
                <Card key={program.id} as="article" className="catalog-card">
                  <CatalogMeta>{t('programs.programLabel')}</CatalogMeta>
                  <h2 dir="auto"><Link to={`/programs/${program.slug}`}>{program.title}</Link></h2>
                  <p dir="auto">{program.description}</p>
                  <Link className="catalog-card__link" to={`/programs/${program.slug}`}>{t('common.viewDetails')}</Link>
                </Card>
              ))}
            </div>
          ) : null}
        </Container>
      </section>
    </>
  )
}

export default ProgramsPage
