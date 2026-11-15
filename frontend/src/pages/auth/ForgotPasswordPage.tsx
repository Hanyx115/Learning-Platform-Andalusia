import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { forgotPassword, getApiErrorMessage } from '../../api/auth'
import AuthPageShell from './AuthPageShell'

function ForgotPasswordPage() {
  const { t } = useTranslation()
  const [email, setEmail] = useState('')
  const [resetToken, setResetToken] = useState<string | null>(null)
  const [isComplete, setIsComplete] = useState(false)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const response = await forgotPassword(email)
      setResetToken(response.resetToken)
      setIsComplete(true)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('auth.errors.generic')))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthPageShell
      eyebrow={t('auth.forgotPassword.eyebrow')}
      title={t('auth.forgotPassword.title')}
      description={t('auth.forgotPassword.description')}
    >
      {isComplete ? (
        <div className="auth-result">
          <p className="auth-message auth-message--success" role="status">
            {t('auth.forgotPassword.success')}
          </p>
          {resetToken ? (
            <Link
              className="button button--primary"
              to={`/reset-password?email=${encodeURIComponent(email)}&token=${encodeURIComponent(resetToken)}`}
            >
              {t('auth.actions.continueReset')}
            </Link>
          ) : null}
          <Link to="/login">{t('auth.actions.backToLogin')}</Link>
        </div>
      ) : (
        <form className="auth-form" onSubmit={handleSubmit}>
          {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
          <label>
            {t('auth.fields.email')}
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
          </label>
          <button className="button button--primary auth-form__submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? t('auth.actions.sending') : t('auth.actions.sendReset')}
          </button>
          <Link to="/login">{t('auth.actions.backToLogin')}</Link>
        </form>
      )}
    </AuthPageShell>
  )
}

export default ForgotPasswordPage
