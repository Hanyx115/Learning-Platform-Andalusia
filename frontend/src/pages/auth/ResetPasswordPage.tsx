import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, resetPassword } from '../../api/auth'
import AuthPageShell from './AuthPageShell'

function ResetPasswordPage() {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const [email, setEmail] = useState(searchParams.get('email') ?? '')
  const [token, setToken] = useState(searchParams.get('token') ?? '')
  const [isComplete, setIsComplete] = useState(false)
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')

    const formData = new FormData(event.currentTarget)
    const newPassword = String(formData.get('newPassword') ?? '')
    const confirmPassword = String(formData.get('confirmPassword') ?? '')

    if (newPassword !== confirmPassword) {
      setError(t('auth.errors.passwordMismatch'))
      return
    }

    setIsSubmitting(true)

    try {
      await resetPassword(email, token, newPassword)
      setIsComplete(true)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('auth.errors.resetPassword')))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthPageShell
      eyebrow={t('auth.resetPassword.eyebrow')}
      title={t('auth.resetPassword.title')}
      description={t('auth.resetPassword.description')}
    >
      {isComplete ? (
        <div className="auth-result">
          <p className="auth-message auth-message--success" role="status">
            {t('auth.resetPassword.success')}
          </p>
          <Link className="button button--primary" to="/login">{t('auth.actions.signIn')}</Link>
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
          <label>
            {t('auth.fields.resetToken')}
            <textarea value={token} onChange={(event) => setToken(event.target.value)} rows={3} required />
          </label>
          <label>
            {t('auth.fields.newPassword')}
            <input name="newPassword" type="password" autoComplete="new-password" minLength={8} required />
            <span className="auth-form__hint">{t('auth.fields.passwordHint')}</span>
          </label>
          <label>
            {t('auth.fields.confirmPassword')}
            <input name="confirmPassword" type="password" autoComplete="new-password" minLength={8} required />
          </label>

          <button className="button button--primary auth-form__submit" type="submit" disabled={isSubmitting}>
            {isSubmitting ? t('auth.actions.saving') : t('auth.actions.resetPassword')}
          </button>
        </form>
      )}
    </AuthPageShell>
  )
}

export default ResetPasswordPage
