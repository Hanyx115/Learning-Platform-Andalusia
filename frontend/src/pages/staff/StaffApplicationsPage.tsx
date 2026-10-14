import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { acceptApplication, getApplicationsPage, rejectApplication, type Application } from '../../api/enrollment'
import Container from '../../components/Container'
import '../profile/profile-dashboard.css'

function StaffApplicationsPage() {
  const { t } = useTranslation()
  const [applications, setApplications] = useState<Application[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [actioningId, setActioningId] = useState<string | null>(null)
  const [actionErrors, setActionErrors] = useState<Record<string, string>>({})
  const [rejectDrafts, setRejectDrafts] = useState<Record<string, string>>({})

  const loadApplications = useCallback(() => {
    getApplicationsPage({ status: 'Pending', pageSize: 50 })
      .then((page) => setApplications(page.items))
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('staffApplications.errors.load')))
        }
      })
  }, [t])

  useEffect(() => {
    loadApplications()
  }, [loadApplications])

  async function handleAccept(applicationId: string) {
    setActioningId(applicationId)
    setActionErrors((previous) => ({ ...previous, [applicationId]: '' }))

    try {
      await acceptApplication(applicationId)
      setApplications((previous) => previous?.filter((application) => application.id !== applicationId) ?? null)
    } catch (requestError) {
      setActionErrors((previous) => ({
        ...previous,
        [applicationId]: getApiErrorMessage(requestError, t('staffApplications.errors.accept')),
      }))
    } finally {
      setActioningId(null)
    }
  }

  async function handleReject(applicationId: string) {
    const reason = rejectDrafts[applicationId]?.trim()
    if (!reason) {
      setActionErrors((previous) => ({
        ...previous,
        [applicationId]: t('staffApplications.errors.reasonRequired'),
      }))
      return
    }

    setActioningId(applicationId)
    setActionErrors((previous) => ({ ...previous, [applicationId]: '' }))

    try {
      await rejectApplication(applicationId, reason)
      setApplications((previous) => previous?.filter((application) => application.id !== applicationId) ?? null)
    } catch (requestError) {
      setActionErrors((previous) => ({
        ...previous,
        [applicationId]: getApiErrorMessage(requestError, t('staffApplications.errors.reject')),
      }))
    } finally {
      setActioningId(null)
    }
  }

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  return (
    <div className="profile-dashboard">
      <Container>
        <section className="dashboard-section">
          <div className="dashboard-section__heading">
            <h1>{t('staffApplications.title')}</h1>
            <p>{t('staffApplications.description')}</p>
          </div>

          {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

          {applications === null ? (
            <p role="status">{t('staffApplications.loading')}</p>
          ) : applications.length === 0 ? (
            <p>{t('staffApplications.empty')}</p>
          ) : (
            <div className="dashboard-list">
              {applications.map((application) => {
                const title = application.courseTitle ?? application.programTitle ?? ''
                const actionError = actionErrors[application.id]
                const isBusy = actioningId === application.id

                return (
                  <article key={application.id} className="dashboard-card">
                    <div className="dashboard-card__heading">
                      <h2 dir="auto">{title}</h2>
                      <span className="status-badge status-badge--pending">
                        {t(`applications.status.${application.status}`, application.status)}
                      </span>
                    </div>
                    <div className="dashboard-card__meta">
                      <span>
                        {t('applications.submittedOn', {
                          date: new Date(application.submittedAt).toLocaleDateString(),
                        })}
                      </span>
                    </div>

                    {actionError ? <p className="auth-message auth-message--error" role="alert">{actionError}</p> : null}

                    <label>
                      {t('staffApplications.rejectionReasonLabel')}
                      <textarea
                        rows={2}
                        value={rejectDrafts[application.id] ?? ''}
                        onChange={(event) =>
                          setRejectDrafts((previous) => ({ ...previous, [application.id]: event.target.value }))
                        }
                      />
                    </label>

                    <div className="dashboard-card__actions">
                      <button
                        className="button button--primary"
                        type="button"
                        disabled={isBusy}
                        onClick={() => void handleAccept(application.id)}
                      >
                        {t('staffApplications.accept')}
                      </button>
                      <button
                        className="button button--secondary"
                        type="button"
                        disabled={isBusy}
                        onClick={() => void handleReject(application.id)}
                      >
                        {t('staffApplications.reject')}
                      </button>
                    </div>
                  </article>
                )
              })}
            </div>
          )}
        </section>
      </Container>
    </div>
  )
}

export default StaffApplicationsPage
