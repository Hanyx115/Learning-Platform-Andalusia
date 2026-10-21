import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getAllPayments, type AdminPayment } from '../../api/admin'
import Container from '../../components/Container'

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Succeeded':
      return 'status-badge status-badge--positive'
    case 'Failed':
      return 'status-badge status-badge--negative'
    default:
      return 'status-badge status-badge--neutral'
  }
}

function AdminPaymentsPage() {
  const { t } = useTranslation()
  const [payments, setPayments] = useState<AdminPayment[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getAllPayments()
      .then((loaded) => {
        if (isCurrent) {
          setPayments(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminPayments.errors.load')))
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
        <h1>{t('adminPayments.title')}</h1>
        <p>{t('adminPayments.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {payments === null ? (
        <p role="status">{t('adminPayments.loading')}</p>
      ) : payments.length === 0 ? (
        <p>{t('adminPayments.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {payments.map((payment) => (
            <article key={payment.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{payment.learnerName}</h2>
                <span className={statusBadgeClass(payment.status)}>{payment.status}</span>
              </div>
              <div className="dashboard-card__meta">
                <span>{payment.email}</span>
                <span dir="auto">{payment.courseTitle ?? payment.programTitle}</span>
                <span>{payment.amount} {payment.currency}</span>
                <span>{new Date(payment.createdAt).toLocaleDateString()}</span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default AdminPaymentsPage
