import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { useCareerPath } from '../../api/catalogQueries'
import { ApiError } from '../../api/client'
import CatalogPhoto from '../../components/CatalogPhoto'
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
            <CatalogPhoto
              imageUrl={query.data.imageUrl}
              slug={query.data.slug}
              className="catalog-detail__image"
              alt={query.data.title}
            />
            <div className="career-plan">
              <header className="career-plan__intro">
                <span className="career-plan__eyebrow">{t('careerPaths.planEyebrow')}</span>
                <h2>{t('careerPaths.planTitle')}</h2>
                <p>{t('careerPaths.planDescription')}</p>
              </header>

              <section className="career-plan__section">
                <header className="career-plan__section-header">
                  <span className="career-plan__step" aria-hidden="true">1</span>
                  <div>
                    <h2>{t('careerPaths.recommendedSkills')}</h2>
                    <p>{t('careerPaths.skillsDescription')}</p>
                  </div>
                  <span className="career-plan__count">
                    {t('careerPaths.itemCount', { count: skills.length })}
                  </span>
                </header>
                {skills.length > 0 ? (
                  <ul className="catalog-skill-list">
                    {skills.map((skill) => (
                      <li key={skill} dir="auto">
                        <span aria-hidden="true">✓</span>
                        {skill}
                      </li>
                    ))}
                  </ul>
                ) : <QueryState kind="empty" />}
              </section>

              <section className="career-plan__section">
                <header className="career-plan__section-header">
                  <span className="career-plan__step" aria-hidden="true">2</span>
                  <div>
                    <h2>{t('careerPaths.recommendedPrograms')}</h2>
                    <p>{t('careerPaths.programsDescription')}</p>
                  </div>
                  <span className="career-plan__count">
                    {t('careerPaths.itemCount', { count: query.data.recommendedPrograms.length })}
                  </span>
                </header>
                {query.data.recommendedPrograms.length > 0 ? (
                  <ul className="career-recommendation-grid">
                    {query.data.recommendedPrograms.map((program) => (
                      <li key={program.programId}>
                        <Link
                          className="career-recommendation-card"
                          aria-label={program.programTitle}
                          to={`/programs/${program.programSlug}`}
                        >
                          <span className="career-recommendation-card__type">{t('careerPaths.programLabel')}</span>
                          <strong dir="auto">{program.programTitle}</strong>
                          <span className="career-recommendation-card__action">
                            {t('careerPaths.viewProgram')}
                            <span aria-hidden="true">→</span>
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                ) : <QueryState kind="empty" />}
              </section>

              <section className="career-plan__section">
                <header className="career-plan__section-header">
                  <span className="career-plan__step" aria-hidden="true">3</span>
                  <div>
                    <h2>{t('careerPaths.recommendedCourses')}</h2>
                    <p>{t('careerPaths.coursesDescription')}</p>
                  </div>
                  <span className="career-plan__count">
                    {t('careerPaths.itemCount', { count: query.data.recommendedCourses.length })}
                  </span>
                </header>
                {query.data.recommendedCourses.length > 0 ? (
                  <ul className="career-recommendation-grid">
                    {query.data.recommendedCourses.map((course) => (
                      <li key={course.courseId}>
                        <Link
                          className="career-recommendation-card"
                          aria-label={course.courseTitle}
                          to={`/courses/${course.courseSlug}`}
                        >
                          <span className="career-recommendation-card__type">{t('careerPaths.courseLabel')}</span>
                          <strong dir="auto">{course.courseTitle}</strong>
                          <span className="career-recommendation-card__action">
                            {t('careerPaths.viewCourse')}
                            <span aria-hidden="true">→</span>
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                ) : <QueryState kind="empty" />}
              </section>
            </div>
            <Link className="button button--secondary career-plan__back" to="/career-paths">
              <span aria-hidden="true">←</span>
              {t('careerPaths.backToCareerPaths')}
            </Link>
          </article>
        </Container>
      </section>
    </>
  )
}

export default CareerPathDetailPage
