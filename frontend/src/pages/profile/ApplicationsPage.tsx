import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyApplications, payForApplication, type Application } from '../../api/enrollment'
import { Link } from 'react-router-dom'
import Container from '../../components/Container'

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Accepted':
      return 'status-badge status-badge--positive'
    case 'Rejected':
      return 'status-badge status-badge--negative'
    default:
      return 'status-badge status-badge--pending'
  }
}

function ApplicationsPage() {
  const { t } = useTranslation()
  const [applications, setApplications] = useState<Application[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [payingId, setPayingId] = useState<string | null>(null)
  const [payErrors, setPayErrors] = useState<Record<string, string>>({})
  const [paidIds, setPaidIds] = useState<Set<string>>(new Set())

  const loadApplications = useCallback(() => {
    getMyApplications()
      .then(setApplications)
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('applications.errors.load')))
        }
      })
  }, [t])

  useEffect(() => {
    loadApplications()
  }, [loadApplications])

  async function handlePay(applicationId: string) {
    setPayingId(applicationId)
    setPayErrors((previous) => ({ ...previous, [applicationId]: '' }))

    try {
      const result = await payForApplication(applicationId)
      if (result.succeeded) {
        setPaidIds((previous) => new Set(previous).add(applicationId))
      } else {
        setPayErrors((previous) => ({
          ...previous,
          [applicationId]: result.error ?? t('applications.errors.pay'),
        }))
      }
    } catch (requestError) {
      setPayErrors((previous) => ({
        ...previous,
        [applicationId]: getApiErrorMessage(requestError, t('applications.errors.pay')),
      }))
    } finally {
      setPayingId(null)
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
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('applications.title')}</h1>
        <p>{t('applications.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {applications === null ? (
        <p role="status">{t('applications.loading')}</p>
      ) : applications.length === 0 ? (
        <p>{t('applications.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {applications.map((application) => {
            const title = application.courseTitle ?? application.programTitle ?? ''
            const isPaid = paidIds.has(application.id)
            const payError = payErrors[application.id]

            return (
              <article key={application.id} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h2 dir="auto">{title}</h2>
                  <span className={statusBadgeClass(application.status)}>
                    {t(`applications.status.${application.status}`, application.status)}
                  </span>
                </div>
                <div className="dashboard-card__meta">
                  <span>
                    {t('applications.submittedOn', {
                      date: new Date(application.submittedAt).toLocaleDateString(),
                    })}
                  </span>
                  {application.status === 'Rejected' && application.rejectionReason ? (
                    <span>{t('applications.rejectionReason', { reason: application.rejectionReason })}</span>
                  ) : null}
                </div>

                {payError ? <p className="auth-message auth-message--error" role="alert">{payError}</p> : null}

                {application.status === 'Accepted' && !isPaid ? (
                  <div className="dashboard-card__actions">
                    <button
                      className="button button--primary"
                      type="button"
                      disabled={payingId === application.id}
                      onClick={() => void handlePay(application.id)}
                    >
                      {payingId === application.id ? t('applications.paying') : t('applications.pay')}
                    </button>
                  </div>
                ) : null}

                {isPaid ? (
                  <div className="dashboard-card__actions">
                    <p className="auth-message auth-message--success" role="status">
                      {t('applications.paid')}
                    </p>
                    <Link className="button button--secondary" to="/profile/enrollments">
                      {t('applications.viewEnrollments')}
                    </Link>
                  </div>
                ) : null}
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}

export default ApplicationsPage
