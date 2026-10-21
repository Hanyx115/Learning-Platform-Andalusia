import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyInstructorSessions, type InstructorSession } from '../../api/instructor'
import Container from '../../components/Container'

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Cancelled':
      return 'status-badge status-badge--negative'
    case 'Completed':
      return 'status-badge status-badge--neutral'
    default:
      return 'status-badge status-badge--positive'
  }
}

function InstructorSessionsPage() {
  const { i18n, t } = useTranslation()
  const [sessions, setSessions] = useState<InstructorSession[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getMyInstructorSessions()
      .then((loaded) => {
        if (isCurrent) {
          setSessions(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorSessions.errors.load')))
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
        <h1>{t('instructorSessions.title')}</h1>
        <p>{t('instructorSessions.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {sessions === null ? (
        <p role="status">{t('instructorSessions.loading')}</p>
      ) : sessions.length === 0 ? (
        <p>{t('instructorSessions.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {sessions.map((session) => (
            <article key={session.sessionId} className="dashboard-card schedule-card">
              <div className="dashboard-card__heading">
                <div>
                  <h2 dir="auto">{session.offeringTitle}</h2>
                  <strong dir="auto">{session.sessionTitle}</strong>
                </div>
                <time dateTime={session.startsAtUtc}>
                  {new Date(session.startsAtUtc).toLocaleString(i18n.language)}
                </time>
                <span className={statusBadgeClass(session.status)}>
                  {t(`instructorSessions.status.${session.status}`, session.status)}
                </span>
              </div>
              <div className="dashboard-card__meta">
                {session.location ? (
                  <span>
                    {t('schedule.location', {
                      branch: session.location.branchName,
                      room: session.location.roomName,
                      city: session.location.city,
                    })}
                  </span>
                ) : (
                  <span>{t('schedule.online')}</span>
                )}
              </div>
              <div className="dashboard-card__actions">
                <Link
                  className="button button--primary"
                  to={`/instructor/sessions/${session.sessionId}/attendance?type=${session.offeringType}`}
                >
                  {t('instructorSessions.markAttendance')}
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default InstructorSessionsPage
