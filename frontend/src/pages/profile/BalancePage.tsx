import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import { addDemoBalanceCredit, getMyBalance, type Balance } from '../../api/compensation'

function BalancePage() {
  const { t, i18n } = useTranslation()
  const [balance, setBalance] = useState<Balance | null>(null)
  const [method, setMethod] = useState('Visa')
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [isWorking, setIsWorking] = useState(false)

  useEffect(() => {
    getMyBalance().then(setBalance).catch((requestError: unknown) => {
      setError(getApiErrorMessage(requestError, t('balance.errors.load')))
    })
  }, [t])

  async function addCredit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const amount = Number(new FormData(form).get('amount'))
    setIsWorking(true)
    setError('')
    setMessage('')
    try {
      setBalance(await addDemoBalanceCredit(amount, method))
      setMessage(t('balance.demo.success'))
      form.reset()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('balance.errors.credit')))
    } finally {
      setIsWorking(false)
    }
  }

  return (
    <section className="dashboard-section">
      <header className="dashboard-section__heading">
        <h1>{t('balance.title')}</h1>
        <p>{t('balance.description')}</p>
      </header>

      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {message ? <p className="auth-message auth-message--success" role="status">{message}</p> : null}

      <article className="balance-card" aria-label={t('balance.current')}>
        <span>{t('balance.current')}</span>
        <strong>
          {balance ? balance.balance.toLocaleString(i18n.language, { minimumFractionDigits: 2 }) : '—'}{' '}
          {balance?.currency ?? 'EGP'}
        </strong>
        <small>{t('balance.privateNote')}</small>
      </article>

      <form className="dashboard-card balance-demo" onSubmit={(event) => void addCredit(event)}>
        <div>
          <h2>{t('balance.demo.title')}</h2>
          <p>{t('balance.demo.description')}</p>
        </div>
        <label>
          {t('balance.demo.amount')}
          <input name="amount" type="number" min="0.01" max="100000" step="0.01" required />
        </label>
        <fieldset className="payment-methods">
          <legend>{t('balance.demo.method')}</legend>
          {['Visa', 'Mastercard', 'Fawry'].map((option) => (
            <label className={`payment-method${method === option ? ' payment-method--selected' : ''}`} key={option}>
              <input type="radio" name="method" value={option} checked={method === option} onChange={() => setMethod(option)} />
              <strong>{option}</strong>
            </label>
          ))}
        </fieldset>
        <button className="button button--primary" type="submit" disabled={isWorking}>
          {isWorking ? t('common.saving') : t('balance.demo.submit')}
        </button>
      </form>
    </section>
  )
}

export default BalancePage
