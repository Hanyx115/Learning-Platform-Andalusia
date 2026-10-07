import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { useProgram } from '../../api/catalogQueries'
import { ApiError } from '../../api/client'
import Container from '../../components/Container'
import { CatalogHero, QueryState } from './CatalogUi'
import './catalog.css'

function ProgramDetailPage() {
  const { t } = useTranslation()
  const { slug = '' } = useParams()
  const query = useProgram(slug)

  if (query.isPending) {
    return <Container><QueryState kind="loading" /></Container>
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.status === 404
    return (
      <Container>
        <div className="catalog-detail-state">
          <h1>{notFound ? t('programs.notFound') : t('common.error')}</h1>
          {!notFound ? <QueryState kind="error" onRetry={() => void query.refetch()} /> : null}
          <Link className="button button--secondary" to="/programs">{t('programs.backToPrograms')}</Link>
        </div>
      </Container>
    )
  }

  return (
    <>
      <CatalogHero eyebrow={t('programs.eyebrow')} title={query.data.title} description={query.data.description} />
      <section className="catalog-page">
        <Container>
          <article className="catalog-detail">
            <h2>{t('programs.includedCourses')}</h2>
            {query.data.courses.length > 0 ? (
              <ol className="catalog-related-list">
                {query.data.courses.map((course) => (
                  <li key={course.courseId}>
                    <span className="catalog-related-list__order">{course.sequenceOrder}</span>
                    <Link dir="auto" to={`/courses/${course.courseSlug}`}>{course.courseTitle}</Link>
                  </li>
                ))}
              </ol>
            ) : <QueryState kind="empty" />}
            <Link className="catalog-card__link" to="/programs">{t('programs.backToPrograms')}</Link>
          </article>
        </Container>
      </section>
    </>
  )
}

export default ProgramDetailPage
