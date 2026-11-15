import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import { getMyInstructorOverview, type InstructorOverview } from '../../api/instructor'

function InstructorOverviewPage() {
  const { t } = useTranslation()
  const [overview, setOverview] = useState<InstructorOverview | null>(null)
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    let isCurrent = true

    getMyInstructorOverview()
      .then((loaded) => {
        if (isCurrent) {
          setOverview(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (isCurrent) {
          setLoadError(getApiErrorMessage(requestError, t('instructorOverview.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [t])

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('instructorOverview.title')}</h1>
        <p>{t('instructorOverview.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {overview === null ? (
        <p role="status">{t('instructorOverview.loading')}</p>
      ) : (
        <>
          <div className="operations-metrics" aria-label={t('instructorOverview.metricsLabel')}>
            <article className="operations-metric">
              <span>{t('instructorOverview.assignedOfferings')}</span>
              <strong>{overview.assignedOfferings.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('instructorOverview.activeStudents')}</span>
              <strong>{overview.activeStudents.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('instructorOverview.upcomingSessions')}</span>
              <strong>{overview.upcomingSessions.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('instructorOverview.pendingSubmissions')}</span>
              <strong>{overview.pendingSubmissions.toLocaleString()}</strong>
            </article>
          </div>

          <div className="dashboard-section__heading operations-subheading">
            <h2>{t('instructorOverview.nextSessions')}</h2>
          </div>
          {overview.nextSessions.length === 0 ? (
            <p>{t('instructorOverview.noSessions')}</p>
          ) : (
            <div className="dashboard-list">
              {overview.nextSessions.map((session) => (
                <article key={session.sessionId} className="dashboard-card schedule-card">
                  <div className="dashboard-card__heading">
                    <div>
                      <h2 dir="auto">{session.sessionTitle}</h2>
                      <span dir="auto">{session.offeringTitle}</span>
                    </div>
                    <time dateTime={session.startsAtUtc}>{new Date(session.startsAtUtc).toLocaleString()}</time>
                  </div>
                </article>
              ))}
            </div>
          )}
        </>
      )}
    </section>
  )
}

export default InstructorOverviewPage
