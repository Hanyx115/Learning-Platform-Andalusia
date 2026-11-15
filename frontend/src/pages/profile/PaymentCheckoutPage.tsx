import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import {
  getMyApplications,
  getMyPayments,
  payForApplication,
  type Application,
  type Payment,
} from '../../api/enrollment'

type PaymentMethod = 'card' | 'fawry'

function PaymentCheckoutPage() {
  const { applicationId } = useParams()
  const { t, i18n } = useTranslation()
  const [application, setApplication] = useState<Application | null>(null)
  const [existingPayment, setExistingPayment] = useState<Payment | null>(null)
  const [method, setMethod] = useState<PaymentMethod>('card')
  const [isLoading, setIsLoading] = useState(true)
  const [isProcessing, setIsProcessing] = useState(false)
  const [loadError, setLoadError] = useState('')
  const [paymentError, setPaymentError] = useState('')
  const [completedPayment, setCompletedPayment] = useState<Payment | null>(null)

  useEffect(() => {
    let isCurrent = true

    Promise.all([getMyApplications(), getMyPayments()])
      .then(([applications, payments]) => {
        if (!isCurrent) {
          return
        }

        setApplication(applications.find((candidate) => candidate.id === applicationId) ?? null)
        setExistingPayment(
          payments.find((candidate) => candidate.applicationId === applicationId && candidate.status === 'Succeeded') ?? null,
        )
      })
      .catch((requestError: unknown) => {
        if (isCurrent) {
          setLoadError(getApiErrorMessage(requestError, t('paymentCheckout.errors.load')))
        }
      })
      .finally(() => {
        if (isCurrent) {
          setIsLoading(false)
        }
      })

    return () => {
      isCurrent = false
    }
  }, [applicationId, t])

  const offeringTitle = useMemo(
    () => application?.courseTitle ?? application?.programTitle ?? t('payments.unknownOffering'),
    [application, t],
  )

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!application || isProcessing) {
      return
    }

    setIsProcessing(true)
    setPaymentError('')

    try {
      // The demo fields are deliberately not sent to the API. The server receives only the
      // application id and remains authoritative for payment and enrollment state.
      const result = await payForApplication(application.id)
      if (!result.succeeded || !result.payment) {
        setPaymentError(result.error ?? t('paymentCheckout.errors.pay'))
        return
      }

      setCompletedPayment(result.payment)
    } catch (requestError) {
      setPaymentError(getApiErrorMessage(requestError, t('paymentCheckout.errors.pay')))
    } finally {
      setIsProcessing(false)
    }
  }

  if (isLoading) {
    return <p role="status">{t('paymentCheckout.loading')}</p>
  }

  if (loadError || !application) {
    return (
      <section className="dashboard-section">
        <p className="auth-message auth-message--error" role="alert">
          {loadError || t('paymentCheckout.errors.notFound')}
        </p>
        <Link className="button button--secondary" to="/profile/applications">
          {t('paymentCheckout.back')}
        </Link>
      </section>
    )
  }

  const successfulPayment = completedPayment ?? existingPayment
  if (successfulPayment) {
    const reference = method === 'fawry'
      ? `FWY-${successfulPayment.id.slice(0, 8).toUpperCase()}`
      : successfulPayment.id.slice(0, 8).toUpperCase()

    return (
      <section className="dashboard-section payment-checkout payment-checkout--complete">
        <div className="payment-checkout__success" role="status">
          <span aria-hidden="true">✓</span>
          <div>
            <h1>{t('paymentCheckout.success.title')}</h1>
            <p>{t('paymentCheckout.success.description')}</p>
            <p>{t('paymentCheckout.success.reference', { reference })}</p>
          </div>
        </div>
        <div className="dashboard-card__actions">
          <Link className="button button--primary" to="/profile/enrollments">
            {t('applications.viewEnrollments')}
          </Link>
          <Link className="button button--secondary" to="/profile/payments">
            {t('paymentCheckout.success.history')}
          </Link>
        </div>
      </section>
    )
  }

  if (application.status !== 'Accepted') {
    return (
      <section className="dashboard-section">
        <p className="auth-message auth-message--error" role="alert">
          {t('paymentCheckout.errors.notAccepted')}
        </p>
        <Link className="button button--secondary" to="/profile/applications">
          {t('paymentCheckout.back')}
        </Link>
      </section>
    )
  }

  return (
    <section className="dashboard-section payment-checkout">
      <header className="dashboard-section__heading">
        <p className="payment-checkout__eyebrow">{t('paymentCheckout.eyebrow')}</p>
        <h1>{t('paymentCheckout.title')}</h1>
        <p>{t('paymentCheckout.description')}</p>
      </header>

      <div className="payment-checkout__layout">
        <form className="payment-checkout__form" onSubmit={(event) => void handleSubmit(event)}>
          <div className="payment-checkout__demo-note" role="note">
            <strong>{t('paymentCheckout.demo.title')}</strong>
            <span>{t('paymentCheckout.demo.description')}</span>
          </div>

          <fieldset className="payment-methods">
            <legend>{t('paymentCheckout.method.legend')}</legend>
            <label className={`payment-method${method === 'card' ? ' payment-method--selected' : ''}`}>
              <input
                type="radio"
                name="paymentMethod"
                value="card"
                checked={method === 'card'}
                onChange={() => setMethod('card')}
              />
              <span>
                <strong>{t('paymentCheckout.method.card')}</strong>
                <small>VISA · Mastercard</small>
              </span>
            </label>
            <label className={`payment-method${method === 'fawry' ? ' payment-method--selected' : ''}`}>
              <input
                type="radio"
                name="paymentMethod"
                value="fawry"
                checked={method === 'fawry'}
                onChange={() => setMethod('fawry')}
              />
              <span>
                <strong>Fawry</strong>
                <small>{t('paymentCheckout.method.fawry')}</small>
              </span>
            </label>
          </fieldset>

          {method === 'card' ? (
            <div className="payment-checkout__fields" key="card-fields">
              <label>
                {t('paymentCheckout.card.name')}
                <input name="cardholder" autoComplete="cc-name" defaultValue="Demo Learner" required />
              </label>
              <label className="payment-checkout__wide-field">
                {t('paymentCheckout.card.number')}
                <input
                  name="cardNumber"
                  inputMode="numeric"
                  autoComplete="cc-number"
                  dir="ltr"
                  defaultValue="4111 1111 1111 1111"
                  pattern="[0-9 ]{15,19}"
                  required
                />
              </label>
              <label>
                {t('paymentCheckout.card.expiry')}
                <input
                  name="expiry"
                  inputMode="numeric"
                  autoComplete="cc-exp"
                  dir="ltr"
                  defaultValue="12/30"
                  pattern="(0[1-9]|1[0-2])/[0-9]{2}"
                  required
                />
              </label>
              <label>
                {t('paymentCheckout.card.securityCode')}
                <input
                  name="securityCode"
                  inputMode="numeric"
                  autoComplete="cc-csc"
                  dir="ltr"
                  defaultValue="123"
                  pattern="[0-9]{3,4}"
                  required
                />
              </label>
            </div>
          ) : (
            <div className="payment-checkout__fawry" key="fawry-fields">
              <h2>{t('paymentCheckout.fawry.title')}</h2>
              <p>{t('paymentCheckout.fawry.description')}</p>
              <label>
                {t('paymentCheckout.fawry.phone')}
                <input
                  name="phoneNumber"
                  type="tel"
                  inputMode="tel"
                  autoComplete="tel"
                  dir="ltr"
                  defaultValue="01000000000"
                  pattern="01[0-9]{9}"
                  required
                />
              </label>
            </div>
          )}

          {paymentError ? <p className="auth-message auth-message--error" role="alert">{paymentError}</p> : null}

          <button className="button button--primary payment-checkout__submit" type="submit" disabled={isProcessing}>
            {isProcessing
              ? t('paymentCheckout.processing')
              : t('paymentCheckout.submit', {
                  amount: application.price.toLocaleString(i18n.language),
                  currency: application.currency,
                })}
          </button>
          <Link className="payment-checkout__back" to="/profile/applications">
            {t('paymentCheckout.back')}
          </Link>
        </form>

        <aside className="payment-summary" aria-labelledby="payment-summary-title">
          <h2 id="payment-summary-title">{t('paymentCheckout.summary.title')}</h2>
          <strong dir="auto">{offeringTitle}</strong>
          <dl>
            <div>
              <dt>{t('paymentCheckout.summary.intake')}</dt>
              <dd>
                {new Date(application.startsAtUtc).toLocaleDateString(i18n.language)} –{' '}
                {new Date(application.endsAtUtc).toLocaleDateString(i18n.language)}
              </dd>
            </div>
            <div className="payment-summary__total">
              <dt>{t('paymentCheckout.summary.total')}</dt>
              <dd>{application.price.toLocaleString(i18n.language)} {application.currency}</dd>
            </div>
          </dl>
        </aside>
      </div>
    </section>
  )
}

export default PaymentCheckoutPage
