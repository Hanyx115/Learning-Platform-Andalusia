import { useCallback, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import { getMyAttendance, type LearnerAttendance } from '../../api/enrollment'

interface AttendanceGroup {
  key: string
  title: string
  records: LearnerAttendance[]
}

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

function groupAttendanceByCourse(records: LearnerAttendance[], fallbackTitle: string) {
  const groups = new Map<string, AttendanceGroup>()

  records.forEach((record) => {
    const title = record.courseTitle ?? record.programTitle ?? fallbackTitle
    const type = record.courseTitle ? 'course' : record.programTitle ? 'program' : 'other'
    const key = `${type}:${title}`
    const group = groups.get(key) ?? { key, title, records: [] }

    if (!group.records.some((groupedRecord) => groupedRecord.sessionId === record.sessionId)) {
      group.records.push(record)
    }

    groups.set(key, group)
  })

  return Array.from(groups.values())
}

function AttendancePage() {
  const { t } = useTranslation()
  const [records, setRecords] = useState<LearnerAttendance[] | null>(null)
  const [error, setError] = useState('')
  const [isRefreshing, setIsRefreshing] = useState(false)
  const isMounted = useRef(true)
  const latestRequestId = useRef(0)
  const groupedRecords = records
    ? groupAttendanceByCourse(records, t('learnerAttendance.uncategorized'))
    : []

  const loadAttendance = useCallback(async (background = false) => {
    const requestId = ++latestRequestId.current
    if (background) setIsRefreshing(true)
    setError('')

    try {
      const loaded = await getMyAttendance()
      if (isMounted.current && requestId === latestRequestId.current) setRecords(loaded)
    } catch (requestError) {
      if (isMounted.current && requestId === latestRequestId.current) {
        setError(getApiErrorMessage(requestError, t('learnerAttendance.errors.load')))
      }
    } finally {
      if (isMounted.current && requestId === latestRequestId.current) {
        setIsRefreshing(false)
      }
    }
  }, [t])

  useEffect(() => {
    isMounted.current = true
    const initialLoad = window.setTimeout(() => void loadAttendance(), 0)

    const refreshWhenVisible = () => {
      if (!document.hidden) void loadAttendance(true)
    }

    window.addEventListener('focus', refreshWhenVisible)
    document.addEventListener('visibilitychange', refreshWhenVisible)
    const refreshInterval = window.setInterval(refreshWhenVisible, 30_000)

    return () => {
      isMounted.current = false
      latestRequestId.current += 1
      window.removeEventListener('focus', refreshWhenVisible)
      document.removeEventListener('visibilitychange', refreshWhenVisible)
      window.clearTimeout(initialLoad)
      window.clearInterval(refreshInterval)
    }
  }, [loadAttendance])

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('learnerAttendance.title')}</h1>
        <p>{t('learnerAttendance.description')}</p>
      </div>
      <div>
        <button
          className="button button--secondary"
          type="button"
          disabled={isRefreshing}
          onClick={() => void loadAttendance(true)}
        >
          {isRefreshing ? t('learnerAttendance.refreshing') : t('learnerAttendance.refresh')}
        </button>
      </div>
      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {records === null ? (
        <p role="status">{t('learnerAttendance.loading')}</p>
      ) : records.length === 0 ? (
        <p>{t('learnerAttendance.empty')}</p>
      ) : (
        <div className="course-record-groups">
          {groupedRecords.map((group, groupIndex) => (
            <section key={group.key} className="course-record-group" aria-labelledby={`attendance-group-${groupIndex}`}>
              <div className="course-record-group__heading">
                <h2 id={`attendance-group-${groupIndex}`} dir="auto">{group.title}</h2>
                <span>{t('learnerAttendance.recordCount', { count: group.records.length })}</span>
              </div>
              <div className="course-record-group__list">
                {group.records.map((record) => (
                  <article key={record.sessionId} className="course-record-item">
                    <div className="dashboard-card__heading">
                      <h3 dir="auto">{record.sessionTitle}</h3>
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
            </section>
          ))}
        </div>
      )}
    </section>
  )
}

export default AttendancePage
