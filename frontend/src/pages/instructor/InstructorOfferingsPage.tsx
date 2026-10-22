import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyInstructorOfferings, type InstructorOffering } from '../../api/instructor'
import Container from '../../components/Container'

function InstructorOfferingsPage() {
  const { t } = useTranslation()
  const [offerings, setOfferings] = useState<InstructorOffering[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getMyInstructorOfferings()
      .then((loaded) => {
        if (isCurrent) {
          setOfferings(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorOfferings.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [t])

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('instructorOfferings.title')}</h1>
        <p>{t('instructorOfferings.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {offerings === null ? (
        <p role="status">{t('instructorOfferings.loading')}</p>
      ) : offerings.length === 0 ? (
        <p>{t('instructorOfferings.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {offerings.map((offering) => {
            const offeringId = offering.courseOfferingId ?? offering.programOfferingId ?? ''
            return (
              <article key={offeringId} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h2 dir="auto">{offering.title}</h2>
                  <span className="status-badge status-badge--neutral">
                    {t('instructorOfferings.studentCount', { count: offering.enrolledStudentCount })}
                  </span>
                </div>
                <div className="dashboard-card__meta">
                  <span>
                    {t('offerings.dates', {
                      start: new Date(offering.startsAtUtc).toLocaleDateString(),
                      end: new Date(offering.endsAtUtc).toLocaleDateString(),
                    })}
                  </span>
                </div>
                <div className="dashboard-card__actions">
                  <Link
                    className="button button--primary"
                    to={`/instructor/offerings/${offeringId}/assignments?type=${offering.offeringType}`}
                  >
                    {t('instructorOfferings.viewAssignments')}
                  </Link>
                  <Link
                    className="button button--secondary"
                    to={`/instructor/offerings/${offeringId}/progress?type=${offering.offeringType}`}
                  >
                    {t('instructorOfferings.viewProgress')}
                  </Link>
                  {offering.courseId ? (
                    <Link
                      className="button button--secondary"
                      to={`/instructor/courses/${offering.courseId}/materials`}
                    >
                      {t('instructorOfferings.manageMaterials')}
                    </Link>
                  ) : null}
                </div>
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}

export default InstructorOfferingsPage
