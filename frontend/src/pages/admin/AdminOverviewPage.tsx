import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getOperationsOverview, type OperationsOverview, type StatusSummary } from '../../api/admin'
import { getApiErrorMessage } from '../../api/auth'

function StatusReport({ title, items }: { title: string; items: StatusSummary[] }) {
  return (
    <article className="dashboard-card operations-report">
      <h2>{title}</h2>
      {items.length === 0 ? (
        <p>—</p>
      ) : (
        <dl>
          {items.map((item) => (
            <div key={item.status}>
              <dt dir="auto">{item.status}</dt>
              <dd>{item.count.toLocaleString()}</dd>
            </div>
          ))}
        </dl>
      )}
    </article>
  )
}

function AdminOverviewPage() {
  const { t } = useTranslation()
  const [overview, setOverview] = useState<OperationsOverview | null>(null)
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    let isCurrent = true

    getOperationsOverview()
      .then((loaded) => {
        if (isCurrent) {
          setOverview(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (isCurrent) {
          setLoadError(getApiErrorMessage(requestError, t('operationsDashboard.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [t])

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('operationsDashboard.title')}</h1>
        <p>{t('operationsDashboard.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {overview === null ? (
        <p role="status">{t('operationsDashboard.loading')}</p>
      ) : (
        <>
          <div className="operations-metrics" aria-label={t('operationsDashboard.metricsLabel')}>
            <article className="operations-metric">
              <span>{t('operationsDashboard.activeStudents')}</span>
              <strong>{overview.activeStudents.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('operationsDashboard.activeInstructors')}</span>
              <strong>{overview.activeInstructors.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('operationsDashboard.runningCourses')}</span>
              <strong>{overview.runningCourses.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('operationsDashboard.pendingApplications')}</span>
              <strong>{overview.pendingApplications.toLocaleString()}</strong>
            </article>
            <article className="operations-metric">
              <span>{t('operationsDashboard.activeEnrollments')}</span>
              <strong>{overview.activeEnrollments.toLocaleString()}</strong>
            </article>
            <article className="operations-metric operations-metric--revenue">
              <span>{t('operationsDashboard.revenue')}</span>
              <strong>
                {overview.revenue.length === 0
                  ? t('operationsDashboard.noRevenue')
                  : overview.revenue
                      .map((item) => `${item.amount.toLocaleString()} ${item.currency}`)
                      .join(' · ')}
              </strong>
            </article>
          </div>

          <div className="dashboard-section__heading operations-subheading">
            <h2>{t('operationsDashboard.reportsTitle')}</h2>
            <p>{t('operationsDashboard.reportsDescription')}</p>
          </div>
          <div className="operations-reports">
            <StatusReport title={t('operationsDashboard.applications')} items={overview.applicationsByStatus} />
            <StatusReport title={t('operationsDashboard.payments')} items={overview.paymentsByStatus} />
            <StatusReport title={t('operationsDashboard.enrollments')} items={overview.enrollmentsByStatus} />
          </div>

          <div className="dashboard-section__heading operations-subheading">
            <h2>{t('operationsDashboard.recentActivity')}</h2>
          </div>
          {overview.recentActivity.length === 0 ? (
            <p>{t('operationsDashboard.noActivity')}</p>
          ) : (
            <div className="dashboard-list">
              {overview.recentActivity.map((activity) => (
                <article key={`${activity.activityType}-${activity.id}`} className="dashboard-card operations-activity">
                  <div>
                    <strong dir="auto">{activity.learnerName}</strong>
                    <span dir="auto">{activity.offeringTitle}</span>
                  </div>
                  <div>
                    <span>{activity.activityType}</span>
                    <span className="status-badge status-badge--neutral" dir="auto">{activity.status}</span>
                    <time dateTime={activity.occurredAtUtc}>
                      {new Date(activity.occurredAtUtc).toLocaleString()}
                    </time>
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

export default AdminOverviewPage
