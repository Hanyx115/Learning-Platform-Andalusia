import { useTranslation } from 'react-i18next'
import type { CourseSession } from '../../api/catalog'

interface SessionScheduleProps {
  sessions: CourseSession[]
}

function SessionSchedule({ sessions }: SessionScheduleProps) {
  const { i18n, t } = useTranslation()

  return (
    <section className="session-schedule" aria-labelledby="session-schedule-title">
      <h2 id="session-schedule-title">{t('schedule.title')}</h2>
      {sessions.length === 0 ? (
        <p>{t('schedule.empty')}</p>
      ) : (
        <ol className="session-schedule__list">
          {sessions.map((session) => (
            <li key={session.id}>
              <div>
                <strong dir="auto">{session.title}</strong>
                <span>
                  {new Date(session.startsAtUtc).toLocaleString(i18n.language)}
                  {' – '}
                  {new Date(session.endsAtUtc).toLocaleTimeString(i18n.language)}
                </span>
              </div>
              <div>
                <span>
                  {t('schedule.location', {
                    branch: session.location.branchName,
                    room: session.location.roomName,
                    city: session.location.city,
                  })}
                </span>
                {session.instructorName ? (
                  <span>{t('schedule.instructor', { name: session.instructorName })}</span>
                ) : null}
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

export default SessionSchedule
