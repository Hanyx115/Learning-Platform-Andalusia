import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useCareerPaths } from '../../api/catalogQueries'
import Card from '../../components/Card'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, QueryState } from './CatalogUi'
import './catalog.css'

function CareerPathsPage() {
  const { t } = useTranslation()
  const query = useCareerPaths()
  const paths = query.data?.filter((path) => path.status === 'Published') ?? []

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
          ) : null}
        </Container>
      </section>
    </>
  )
}

export default CareerPathsPage
