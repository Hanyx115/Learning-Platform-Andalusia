import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyUpcomingSessions, type UpcomingSession } from '../../api/enrollment'
import Container from '../../components/Container'

interface ScheduleGroup {
  key: string
  title: string
  sessions: UpcomingSession[]
}

function groupSessionsByCourse(sessions: UpcomingSession[]) {
  const groups = new Map<string, ScheduleGroup>()

  sessions.forEach((session) => {
    const key = `${session.offeringType}:${session.offeringTitle.trim().toLocaleLowerCase()}`
    const group = groups.get(key) ?? { key, title: session.offeringTitle, sessions: [] }

    if (!group.sessions.some((groupedSession) => groupedSession.sessionId === session.sessionId)) {
      group.sessions.push(session)
    }

    groups.set(key, group)
  })

  return Array.from(groups.values())
}

function SchedulePage() {
  const { i18n, t } = useTranslation()
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone
  const [sessions, setSessions] = useState<UpcomingSession[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const groupedSessions = sessions ? groupSessionsByCourse(sessions) : []

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
        <div className="course-record-groups">
          {groupedSessions.map((group, groupIndex) => (
            <section key={group.key} className="course-record-group" aria-labelledby={`schedule-group-${groupIndex}`}>
              <div className="course-record-group__heading">
                <h2 id={`schedule-group-${groupIndex}`} dir="auto">{group.title}</h2>
                <span>{t('schedule.sessionCount', { count: group.sessions.length })}</span>
              </div>
              <div className="course-record-group__list">
                {group.sessions.map((session) => (
                  <article key={session.sessionId} className="course-record-item schedule-card">
                    <div className="dashboard-card__heading">
                      <h3 dir="auto">{session.sessionTitle}</h3>
                      <div className="course-record-item__status">
                        <time dateTime={session.startsAtUtc}>
                          {new Date(session.startsAtUtc).toLocaleString(i18n.language)}
                        </time>
                        {session.status === 'Cancelled' ? (
                          <span className="status-badge status-badge--negative">{t('schedule.cancelled')}</span>
                        ) : null}
                      </div>
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
            </section>
          ))}
        </div>
      )}
    </section>
  )
}

export default SchedulePage
