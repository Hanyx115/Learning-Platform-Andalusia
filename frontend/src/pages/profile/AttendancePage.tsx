import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import { getMyAttendance, type LearnerAttendance } from '../../api/enrollment'

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Present':
      return 'status-badge status-badge--positive'
    case 'Absent':
      return 'status-badge status-badge--negative'
    default:
      return 'status-badge status-badge--neutral'
  }
}

function AttendancePage() {
  const { t } = useTranslation()
  const [records, setRecords] = useState<LearnerAttendance[] | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let isCurrent = true
    getMyAttendance()
      .then((loaded) => {
        if (isCurrent) setRecords(loaded)
      })
      .catch((requestError: unknown) => {
        if (isCurrent) setError(getApiErrorMessage(requestError, t('learnerAttendance.errors.load')))
      })

    return () => { isCurrent = false }
  }, [t])

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('learnerAttendance.title')}</h1>
        <p>{t('learnerAttendance.description')}</p>
      </div>
      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {records === null ? (
        <p role="status">{t('learnerAttendance.loading')}</p>
      ) : records.length === 0 ? (
        <p>{t('learnerAttendance.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {records.map((record) => (
            <article key={record.sessionId} className="dashboard-card">
              <div className="dashboard-card__heading">
                <div>
                  <h2 dir="auto">{record.sessionTitle}</h2>
                  <p dir="auto">{record.courseTitle ?? record.programTitle}</p>
                </div>
                <span className={statusBadgeClass(record.status)}>
                  {t(`learnerAttendance.status.${record.status}`, { defaultValue: record.status })}
                </span>
              </div>
              <div className="dashboard-card__meta">
                <span>{new Date(record.startsAtUtc).toLocaleString()}</span>
                {record.note ? <span dir="auto">{record.note}</span> : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default AttendancePage
