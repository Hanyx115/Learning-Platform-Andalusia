import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { useCourse } from '../../api/catalogQueries'
import { ApiError } from '../../api/client'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, QueryState } from './CatalogUi'
import './catalog.css'

function CourseDetailPage() {
  const { t } = useTranslation()
  const { slug = '' } = useParams()
  const query = useCourse(slug)

  if (query.isPending) {
    return <Container><QueryState kind="loading" /></Container>
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.status === 404
    return (
      <Container>
        <div className="catalog-detail-state">
          <h1>{notFound ? t('courses.notFound') : t('common.error')}</h1>
          {!notFound ? <QueryState kind="error" onRetry={() => void query.refetch()} /> : null}
          <Link className="button button--secondary" to="/courses">{t('courses.backToCourses')}</Link>
        </div>
      </Container>
    )
  }

  return (
    <>
      <CatalogHero eyebrow={t('courses.eyebrow')} title={query.data.title} description={query.data.description} />
      <section className="catalog-page">
        <Container>
          <article className="catalog-detail">
            <CatalogPhoto
              categoryName={query.data.categoryName}
              slug={query.data.slug}
              className="catalog-detail__image"
              alt={query.data.title}
            />
            <div className="catalog-card__meta">
              <CatalogMeta>{query.data.categoryName}</CatalogMeta>
              <CatalogMeta>{query.data.type}</CatalogMeta>
              <CatalogMeta>{query.data.status}</CatalogMeta>
            </div>
            <p dir="auto">{query.data.description}</p>
            <Link className="catalog-card__link" to="/courses">{t('courses.backToCourses')}</Link>
          </article>
        </Container>
      </section>
    </>
  )
}

export default CourseDetailPage
