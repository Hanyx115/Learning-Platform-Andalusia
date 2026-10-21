import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getAllEnrollments, type AdminEnrollment } from '../../api/admin'
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

function AdminStudentsPage() {
  const { t } = useTranslation()
  const [enrollments, setEnrollments] = useState<AdminEnrollment[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getAllEnrollments()
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
          setLoadError(getApiErrorMessage(requestError, t('adminStudents.errors.load')))
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
        <h1>{t('adminStudents.title')}</h1>
        <p>{t('adminStudents.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {enrollments === null ? (
        <p role="status">{t('adminStudents.loading')}</p>
      ) : enrollments.length === 0 ? (
        <p>{t('adminStudents.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {enrollments.map((enrollment) => (
            <article key={enrollment.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{enrollment.learnerName}</h2>
                <span className={statusBadgeClass(enrollment.status)}>{enrollment.status}</span>
              </div>
              <div className="dashboard-card__meta">
                <span>{enrollment.email}</span>
                <span dir="auto">{enrollment.courseTitle ?? enrollment.programTitle}</span>
                <span>
                  {t('enrollments.enrolledOn', { date: new Date(enrollment.enrolledAt).toLocaleDateString() })}
                </span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default AdminStudentsPage
