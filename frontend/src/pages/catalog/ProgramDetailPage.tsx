import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage } from '../../api/auth'
import { useProgram, useProgramOfferings } from '../../api/catalogQueries'
import { ApiError } from '../../api/client'
import { submitApplication } from '../../api/enrollment'
import { useAuthProfile } from '../../auth/profileContext'
import { useAuthSession } from '../../auth/session'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, QueryState } from './CatalogUi'
import OfferingSelector from './OfferingSelector'
import './catalog.css'

function ProgramDetailPage() {
  const { t } = useTranslation()
  const { slug = '' } = useParams()
  const query = useProgram(slug)
  const offeringsQuery = useProgramOfferings(slug)
  const tokens = useAuthSession()
  const { profile } = useAuthProfile()
  const isLearner = profile?.roles.includes('Learner') ?? false
  const [isApplying, setIsApplying] = useState(false)
  const [applyError, setApplyError] = useState('')
  const [hasApplied, setHasApplied] = useState(false)
  const [selectedOfferingId, setSelectedOfferingId] = useState('')

  async function handleApply() {
    const offeringId = selectedOfferingId || offeringsQuery.data?.[0]?.id
    if (!offeringId) {
      return
    }

    setIsApplying(true)
    setApplyError('')

    try {
      await submitApplication({ programOfferingId: offeringId })
      setHasApplied(true)
    } catch (requestError) {
      setApplyError(getApiErrorMessage(requestError, t('programs.applyError')))
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
            <CatalogPhoto
              slug={query.data.slug}
              className="catalog-detail__image"
              alt={query.data.title}
            />
            {offeringsQuery.isPending ? <QueryState kind="loading" /> : null}
            {offeringsQuery.isError ? (
              <QueryState kind="error" onRetry={() => void offeringsQuery.refetch()} />
            ) : null}
            {offeringsQuery.data?.length ? (
              <OfferingSelector
                offerings={offeringsQuery.data}
                selectedId={selectedOfferingId || offeringsQuery.data[0].id}
                onSelect={setSelectedOfferingId}
              />
            ) : offeringsQuery.isSuccess ? <p>{t('offerings.empty')}</p> : null}

            {offeringsQuery.data?.length && (!tokens || isLearner) ? (
              <div className="catalog-detail__apply">
                {!tokens ? (
                  <Link className="button button--primary" to="/login">{t('programs.signInToApply')}</Link>
                ) : hasApplied ? (
                  <p className="auth-message auth-message--success" role="status">{t('programs.applySuccess')}</p>
                ) : (
                  <button
                    className="button button--primary"
                    type="button"
                    disabled={isApplying}
                    onClick={() => void handleApply()}
                  >
                    {isApplying ? t('programs.applying') : t('programs.apply')}
                  </button>
                )}
                {applyError ? <p className="auth-message auth-message--error" role="alert">{applyError}</p> : null}
              </div>
            ) : null}

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
