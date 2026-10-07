import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { useCareerPath } from '../../api/catalogQueries'
import { ApiError } from '../../api/client'
import Container from '../../components/Container'
import { CatalogHero, QueryState } from './CatalogUi'
import './catalog.css'

function CareerPathDetailPage() {
  const { t } = useTranslation()
  const { slug = '' } = useParams()
  const query = useCareerPath(slug)

  if (query.isPending) {
    return <Container><QueryState kind="loading" /></Container>
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.status === 404
    return (
      <Container>
        <div className="catalog-detail-state">
          <h1>{notFound ? t('careerPaths.notFound') : t('common.error')}</h1>
          {!notFound ? <QueryState kind="error" onRetry={() => void query.refetch()} /> : null}
          <Link className="button button--secondary" to="/career-paths">{t('careerPaths.backToCareerPaths')}</Link>
        </div>
      </Container>
    )
  }

  const skills = query.data.recommendedSkills.split(',').map((skill) => skill.trim()).filter(Boolean)

  return (
    <>
      <CatalogHero eyebrow={t('careerPaths.eyebrow')} title={query.data.title} description={query.data.description} />
      <section className="catalog-page">
        <Container>
          <article className="catalog-detail catalog-detail--sections">
            <section>
              <h2>{t('careerPaths.recommendedSkills')}</h2>
              {skills.length > 0 ? (
                <ul className="catalog-skill-list">
                  {skills.map((skill) => <li key={skill} dir="auto">{skill}</li>)}
                </ul>
              ) : <QueryState kind="empty" />}
            </section>
            <section>
              <h2>{t('careerPaths.recommendedPrograms')}</h2>
              {query.data.recommendedPrograms.length > 0 ? (
                <ul className="catalog-link-list">
                  {query.data.recommendedPrograms.map((program) => (
                    <li key={program.programId}><Link dir="auto" to={`/programs/${program.programSlug}`}>{program.programTitle}</Link></li>
                  ))}
                </ul>
              ) : <QueryState kind="empty" />}
            </section>
            <section>
              <h2>{t('careerPaths.recommendedCourses')}</h2>
              {query.data.recommendedCourses.length > 0 ? (
                <ul className="catalog-link-list">
                  {query.data.recommendedCourses.map((course) => (
                    <li key={course.courseId}><Link dir="auto" to={`/courses/${course.courseSlug}`}>{course.courseTitle}</Link></li>
                  ))}
                </ul>
              ) : <QueryState kind="empty" />}
            </section>
            <Link className="catalog-card__link" to="/career-paths">{t('careerPaths.backToCareerPaths')}</Link>
          </article>
        </Container>
      </section>
    </>
  )
}

export default CareerPathDetailPage
