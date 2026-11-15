import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { getApiErrorMessage, register } from '../../api/auth'
import { resolveLanguage } from '../../i18n'
import AuthPageShell from './AuthPageShell'

function RegisterPage() {
  const { i18n, t } = useTranslation()
  const navigate = useNavigate()
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')

    const formData = new FormData(event.currentTarget)
    const password = String(formData.get('password') ?? '')
    const confirmPassword = String(formData.get('confirmPassword') ?? '')

    if (password !== confirmPassword) {
      setError(t('auth.errors.passwordMismatch'))
      return
    }

    setIsSubmitting(true)

    try {
      await register({
        email: String(formData.get('email') ?? '').trim(),
        password,
        firstName: String(formData.get('firstName') ?? '').trim(),
        lastName: String(formData.get('lastName') ?? '').trim(),
        phoneNumber: String(formData.get('phoneNumber') ?? '').trim(),
        countryCode: String(formData.get('countryCode') ?? '').trim().toUpperCase(),
        preferredLanguage: resolveLanguage(i18n.resolvedLanguage ?? i18n.language),
      })
      navigate('/profile', { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('auth.errors.register')))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthPageShell
      eyebrow={t('auth.register.eyebrow')}
      title={t('auth.register.title')}
      description={t('auth.register.description')}
    >
      <form className="auth-form" onSubmit={handleSubmit}>
        {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}

        <div className="auth-form__row">
          <label>
            {t('auth.fields.firstName')}
            <input name="firstName" autoComplete="given-name" maxLength={100} required />
          </label>
          <label>
            {t('auth.fields.lastName')}
            <input name="lastName" autoComplete="family-name" maxLength={100} required />
          </label>
        </div>

        <label>
          {t('auth.fields.email')}
          <input name="email" type="email" autoComplete="email" maxLength={256} required />
        </label>

        <div className="auth-form__row">
          <label>
            {t('auth.fields.phoneNumber')}
            <input name="phoneNumber" type="tel" autoComplete="tel" maxLength={32} required />
          </label>
          <label>
            {t('auth.fields.countryCode')}
            <input name="countryCode" autoComplete="country" defaultValue="EG" minLength={2} maxLength={2} required />
          </label>
        </div>

        <label>
          {t('auth.fields.password')}
          <input name="password" type="password" autoComplete="new-password" minLength={8} required />
          <span className="auth-form__hint">{t('auth.fields.passwordHint')}</span>
        </label>

        <label>
          {t('auth.fields.confirmPassword')}
          <input name="confirmPassword" type="password" autoComplete="new-password" minLength={8} required />
        </label>

        <button className="button button--primary auth-form__submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? t('auth.actions.creatingAccount') : t('auth.actions.createAccount')}
        </button>
      </form>

      <p className="auth-card__footer">
        {t('auth.register.hasAccount')} <Link to="/login">{t('auth.actions.signIn')}</Link>
      </p>
    </AuthPageShell>
  )
}

export default RegisterPage
