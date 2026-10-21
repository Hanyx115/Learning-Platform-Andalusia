import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyEnrollments, type Enrollment } from '../../api/enrollment'
import Container from '../../components/Container'

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Active':
      return 'status-badge status-badge--positive'
    case 'Cancelled':
      return 'status-badge status-badge--negative'
    default:
      return 'status-badge status-badge--neutral'
  }
}

function EnrollmentsPage() {
  const { t } = useTranslation()
  const [enrollments, setEnrollments] = useState<Enrollment[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getMyEnrollments()
      .then((loaded) => {
        if (isCurrent) {
          setEnrollments(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('enrollments.errors.load')))
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
        <h1>{t('enrollments.title')}</h1>
        <p>{t('enrollments.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {enrollments === null ? (
        <p role="status">{t('enrollments.loading')}</p>
      ) : enrollments.length === 0 ? (
        <p>{t('enrollments.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {enrollments.map((enrollment) => (
            <article key={enrollment.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{enrollment.courseTitle ?? enrollment.programTitle ?? ''}</h2>
                <span className={statusBadgeClass(enrollment.status)}>
                  {t(`enrollments.status.${enrollment.status}`, enrollment.status)}
                </span>
              </div>
              <div className="dashboard-card__meta">
                <span>
                  {t('enrollments.enrolledOn', {
                    date: new Date(enrollment.enrolledAt).toLocaleDateString(),
                  })}
                </span>
                <span>
                  {t('schedule.location', {
                    branch: enrollment.location.branchName,
                    room: enrollment.location.roomName,
                    city: enrollment.location.city,
                  })}
                </span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default EnrollmentsPage
