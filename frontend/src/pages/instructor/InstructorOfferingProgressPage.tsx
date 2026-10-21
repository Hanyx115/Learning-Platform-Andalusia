import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getOfferingProgress, type OfferingProgressRecord, type OfferingType } from '../../api/instructor'
import Container from '../../components/Container'

function InstructorOfferingProgressPage() {
  const { t } = useTranslation()
  const { offeringId } = useParams()
  const [searchParams] = useSearchParams()
  const offeringType = (searchParams.get('type') as OfferingType | null) ?? 'Course'

  const [records, setRecords] = useState<OfferingProgressRecord[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    if (!offeringId) {
      return () => {
        isCurrent = false
      }
    }

    getOfferingProgress(offeringId, offeringType)
      .then((loaded) => {
        if (isCurrent) {
          setRecords(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorProgress.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [offeringId, offeringType, t])

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
        <h1>{t('instructorProgress.title')}</h1>
        <p>{t('instructorProgress.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/instructor/offerings">
          {t('instructorProgress.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {records === null ? (
        <p role="status">{t('instructorProgress.loading')}</p>
      ) : records.length === 0 ? (
        <p>{t('instructorProgress.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {records.map((record) => (
            <article key={record.userId} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{record.learnerName}</h2>
                <span className="status-badge status-badge--positive">
                  {t('instructorProgress.overall', { percentage: record.overallProgressPercentage })}
                </span>
              </div>
              <div className="dashboard-card__meta">
                <span>{record.email}</span>
                <span>
                  {t('instructorProgress.attendance', {
                    attended: record.attendedSessions,
                    total: record.totalSessions,
                    percentage: record.attendanceRatePercentage,
                  })}
                </span>
                <span>
                  {t('instructorProgress.assignments', {
                    submitted: record.submittedAssignments,
                    total: record.totalAssignments,
                  })}
                </span>
                {record.averageScorePercentage !== null ? (
                  <span>
                    {t('instructorProgress.averageScore', { percentage: record.averageScorePercentage })}
                  </span>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default InstructorOfferingProgressPage
