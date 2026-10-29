import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getMyApplications, getMyPayments, type Application, type Payment } from '../../api/enrollment'

function PaymentHistoryPage() {
  const { t, i18n } = useTranslation()
  const [payments, setPayments] = useState<Payment[] | null>(null)
  const [applications, setApplications] = useState<Application[]>([])
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let isCurrent = true
    Promise.all([getMyPayments(), getMyApplications()])
      .then(([loadedPayments, loadedApplications]) => {
        if (isCurrent) {
          setPayments(loadedPayments)
          setApplications(loadedApplications)
        }
      })
      .catch(() => { if (isCurrent) setFailed(true) })
    return () => { isCurrent = false }
  }, [])

  function offeringTitle(applicationId: string) {
    const application = applications.find((candidate) => candidate.id === applicationId)
    return application?.courseTitle ?? application?.programTitle ?? t('payments.unknownOffering')
  }

  return (
    <section className="dashboard-section">
      <header className="dashboard-section__heading">
        <h1>{t('payments.title')}</h1>
        <p>{t('payments.description')}</p>
      </header>
      {payments === null && !failed ? <p>{t('common.loading')}</p> : null}
      {failed ? <p role="alert">{t('payments.errors.load')}</p> : null}
      {payments?.length === 0 ? <p>{t('payments.empty')}</p> : null}
      <div className="dashboard-list">
        {payments?.map((payment) => (
          <article key={payment.id} className="dashboard-card">
            <div className="dashboard-card__heading">
              <h2>{offeringTitle(payment.applicationId)}</h2>
              <span className={`status-badge ${payment.status === 'Succeeded' ? 'status-badge--positive' : payment.status === 'Failed' ? 'status-badge--negative' : 'status-badge--pending'}`}>
                {payment.status}
              </span>
            </div>
            <p>{payment.amount.toLocaleString(i18n.language)} {payment.currency}</p>
            <div className="application-history">
              <h3>{t('payments.history')}</h3>
              <ol>
                {(payment.statusHistory ?? []).map((entry) => (
                  <li key={entry.id || `${entry.status}-${entry.changedAt}`}>
                    <strong>{entry.status}</strong>
                    <time dateTime={entry.changedAt}>{new Date(entry.changedAt).toLocaleString(i18n.language)}</time>
                  </li>
                ))}
              </ol>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

export default PaymentHistoryPage
