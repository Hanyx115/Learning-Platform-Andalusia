import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyUpcomingSessions, type UpcomingSession } from '../../api/enrollment'
import Container from '../../components/Container'

function SchedulePage() {
  const { i18n, t } = useTranslation()
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone
  const [sessions, setSessions] = useState<UpcomingSession[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getMyUpcomingSessions()
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
          setLoadError(getApiErrorMessage(requestError, t('schedule.errors.load')))
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
        <h1>{t('schedule.myTitle')}</h1>
        <p>{t('schedule.myDescription')}</p>
      </div>

      {loadError ? (
        <p className="auth-message auth-message--error" role="alert">{loadError}</p>
      ) : sessions === null ? (
        <p role="status">{t('schedule.loading')}</p>
      ) : sessions.length === 0 ? (
        <p>{t('schedule.myEmpty')}</p>
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
                {session.status === 'Cancelled' ? (
                  <span className="status-badge status-badge--negative">{t('schedule.cancelled')}</span>
                ) : null}
              </div>
              <div className="dashboard-card__meta">
                <span>{t('schedule.timeZone', { timeZone })}</span>
                {session.location ? <>
                <span>
                  {t('schedule.location', {
                    branch: session.location.branchName,
                    room: session.location.roomName,
                    city: session.location.city,
                  })}
                </span>
                <span>{session.location.address}</span>
                </> : <span>{t('schedule.online')}</span>}
                {session.instructorName ? (
                  <span>{t('schedule.instructor', { name: session.instructorName })}</span>
                ) : null}
                {session.cancellationReason ? (
                  <span>{t('schedule.cancellationReason', { reason: session.cancellationReason })}</span>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default SchedulePage
