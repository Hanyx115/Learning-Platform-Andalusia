import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage } from '../../api/auth'
import { useCourse } from '../../api/catalogQueries'
import { ApiError } from '../../api/client'
import { submitApplication } from '../../api/enrollment'
import { useAuthProfile } from '../../auth/profileContext'
import { useAuthSession } from '../../auth/session'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, QueryState } from './CatalogUi'
import './catalog.css'

function CourseDetailPage() {
  const { t } = useTranslation()
  const { slug = '' } = useParams()
  const query = useCourse(slug)
  const tokens = useAuthSession()
  const { profile } = useAuthProfile()
  const isLearner = profile?.roles.includes('Learner') ?? false
  const [isApplying, setIsApplying] = useState(false)
  const [applyError, setApplyError] = useState('')
  const [hasApplied, setHasApplied] = useState(false)

  async function handleApply(courseId: string) {
    setIsApplying(true)
    setApplyError('')

    try {
      await submitApplication({ courseId })
      setHasApplied(true)
    } catch (requestError) {
      setApplyError(getApiErrorMessage(requestError, t('courses.applyError')))
    } finally {
      setIsApplying(false)
    }
  }

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

            {query.data.price !== null ? (
              <p className="catalog-detail__price">{t('courses.price', { price: query.data.price })}</p>
            ) : null}

            {!tokens || isLearner ? (
              <div className="catalog-detail__apply">
                {!tokens ? (
                  <Link className="button button--primary" to="/login">{t('courses.signInToApply')}</Link>
                ) : hasApplied ? (
                  <p className="auth-message auth-message--success" role="status">{t('courses.applySuccess')}</p>
                ) : (
                  <button
                    className="button button--primary"
                    type="button"
                    disabled={isApplying}
                    onClick={() => void handleApply(query.data.id)}
                  >
                    {isApplying ? t('courses.applying') : t('courses.apply')}
                  </button>
                )}
                {applyError ? <p className="auth-message auth-message--error" role="alert">{applyError}</p> : null}
              </div>
            ) : null}

            <Link className="catalog-card__link" to="/courses">{t('courses.backToCourses')}</Link>
          </article>
        </Container>
      </section>
    </>
  )
}

export default CourseDetailPage
