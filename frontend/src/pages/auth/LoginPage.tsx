import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { getApiErrorMessage, login } from '../../api/auth'
import AuthPageShell from './AuthPageShell'

function LoginPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await login({ email, password })
      const destination = (location.state as { from?: string } | null)?.from ?? '/profile'
      navigate(destination, { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('auth.errors.login')))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthPageShell
      eyebrow={t('auth.login.eyebrow')}
      title={t('auth.login.title')}
      description={t('auth.login.description')}
    >
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
          {t('auth.fields.password')}
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </label>

        <div className="auth-form__meta">
          <Link to="/forgot-password">{t('auth.login.forgotPassword')}</Link>
        </div>

        <button className="button button--primary auth-form__submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('auth.actions.signingIn') : t('auth.actions.signIn')}
        </button>
      </form>

      <p className="auth-card__footer">
        {t('auth.login.noAccount')} <Link to="/register">{t('auth.actions.createAccount')}</Link>
      </p>
    </AuthPageShell>
  )
}

export default LoginPage
