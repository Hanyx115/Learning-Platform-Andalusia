import { useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import {
  getApiErrorMessage,
  getApiErrorStatus,
  getProfile,
  updateProfile,
  type Profile,
} from '../../api/auth'
import Container from '../../components/Container'
import './auth.css'

function ProfilePage() {
  const { t } = useTranslation()
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [isSaved, setIsSaved] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [learnerPetEnabled, setLearnerPetEnabled] = useState(false)
  const [learnerPetType, setLearnerPetType] = useState<'cat' | 'owl' | 'fox'>('cat')

  useEffect(() => {
    let isCurrent = true

    getProfile()
      .then((loadedProfile) => {
        if (isCurrent) {
          setProfile(loadedProfile)
          setLearnerPetEnabled(loadedProfile.learnerPetEnabled ?? false)
          setLearnerPetType(loadedProfile.learnerPetType ?? 'cat')
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('auth.errors.profile')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [t])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaveError('')
    setIsSaved(false)
    setIsSaving(true)

    const formData = new FormData(event.currentTarget)

    try {
      const updatedProfile = await updateProfile({
        firstName: String(formData.get('firstName') ?? '').trim(),
        lastName: String(formData.get('lastName') ?? '').trim(),
        phoneNumber: String(formData.get('phoneNumber') ?? '').trim(),
        countryCode: String(formData.get('countryCode') ?? '').trim().toUpperCase(),
        preferredLanguage: String(formData.get('preferredLanguage') ?? ''),
        learnerPetEnabled,
        learnerPetType,
      })
      setProfile(updatedProfile)
      setLearnerPetEnabled(updatedProfile.learnerPetEnabled ?? false)
      setLearnerPetType(updatedProfile.learnerPetType ?? 'cat')
      window.dispatchEvent(new CustomEvent('andalusia:profile-updated', { detail: updatedProfile }))
      setIsSaved(true)
    } catch (requestError) {
      if (getApiErrorStatus(requestError) === 403) {
        setIsForbidden(true)
      } else {
        setSaveError(getApiErrorMessage(requestError, t('auth.errors.profileSave')))
      }
    } finally {
      setIsSaving(false)
    }
  }

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
        <Link className="button button--primary" to="/">{t('forbidden.returnHome')}</Link>
      </Container>
    )
  }

  if (loadError) {
    return (
      <Container className="auth-state-page">
        <h1>{t('profile.title')}</h1>
        <p className="auth-message auth-message--error" role="alert">{loadError}</p>
      </Container>
    )
  }

  if (!profile) {
    return <Container className="auth-state-page"><p role="status">{t('profile.loading')}</p></Container>
  }

  return (
    <div className="profile-page">
      <Container>
        <div className="profile-page__heading">
          <div>
            <p className="auth-card__eyebrow">
              {t('profile.accountType', {
                roles: profile.roles.map((role) => t(`profile.roleNames.${role}`, role)).join(', '),
              })}
            </p>
            <h1>{t('profile.title')}</h1>
            <p>{t('profile.description')}</p>
          </div>
          <div className="profile-page__identity">
            <strong>{profile.email}</strong>
            <span>{profile.roles.join(', ')}</span>
          </div>
        </div>

        <form className="auth-form profile-form" onSubmit={handleSubmit}>
          {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}
          {isSaved ? <p className="auth-message auth-message--success" role="status">{t('profile.saved')}</p> : null}

          <div className="auth-form__row">
            <label>
              {t('auth.fields.firstName')}
              <input name="firstName" defaultValue={profile.firstName} maxLength={100} required />
            </label>
            <label>
              {t('auth.fields.lastName')}
              <input name="lastName" defaultValue={profile.lastName} maxLength={100} required />
            </label>
          </div>

          <div className="auth-form__row">
            <label>
              {t('auth.fields.phoneNumber')}
              <input name="phoneNumber" type="tel" defaultValue={profile.phoneNumber} maxLength={32} required />
            </label>
            <label>
              {t('auth.fields.countryCode')}
              <input name="countryCode" defaultValue={profile.countryCode} minLength={2} maxLength={2} required />
            </label>
          </div>

          <label>
            {t('auth.fields.preferredLanguage')}
            <select name="preferredLanguage" defaultValue={profile.preferredLanguage} required>
              <option value="ar">{t('language.arabic')}</option>
              <option value="en">{t('language.english')}</option>
            </select>
          </label>

          {profile.roles.includes('Learner') ? (
            <fieldset className="profile-pet-settings">
              <legend className="profile-pet-settings__legend">{t('learnerPet.settingsTitle')}</legend>
              <div className="profile-pet-settings__header">
                <div className="profile-pet-settings__intro">
                  <span className="profile-pet-settings__paw" aria-hidden="true">🐾</span>
                  <div>
                    <h2>{t('learnerPet.settingsTitle')}</h2>
                    <p>{t('learnerPet.settingsDescription')}</p>
                  </div>
                </div>
                <label className="profile-pet-settings__toggle">
                  <span className="profile-pet-settings__toggle-copy">
                    <strong>
                      {learnerPetEnabled ? t('learnerPet.statusEnabled') : t('learnerPet.statusDisabled')}
                    </strong>
                    <small>{t('learnerPet.enableLabel')}</small>
                  </span>
                  <input
                    type="checkbox"
                    checked={learnerPetEnabled}
                    aria-label={t('learnerPet.enableLabel')}
                    onChange={(event) => setLearnerPetEnabled(event.target.checked)}
                  />
                  <span className="profile-pet-settings__switch" aria-hidden="true" />
                </label>
              </div>
              <div className="profile-pet-settings__choice-heading">
                <strong>{t('learnerPet.chooseLabel')}</strong>
                <small>
                  {learnerPetEnabled
                    ? t('learnerPet.selectedCompanion', { pet: t(`learnerPet.types.${learnerPetType}`) })
                    : t('learnerPet.disabledHint')}
                </small>
              </div>
              <div className="profile-pet-settings__choices" aria-label={t('learnerPet.chooseLabel')}>
                {(['cat', 'owl', 'fox'] as const).map((petType) => (
                  <label
                    key={petType}
                    className={learnerPetEnabled && learnerPetType === petType ? 'is-selected' : ''}
                  >
                    <input
                      type="radio"
                      name="learnerPetType"
                      value={petType}
                      checked={learnerPetType === petType}
                      disabled={!learnerPetEnabled}
                      onChange={() => setLearnerPetType(petType)}
                    />
                    <span className="profile-pet-settings__pet" aria-hidden="true">
                      {petType === 'cat' ? '🐱' : petType === 'owl' ? '🦉' : '🦊'}
                    </span>
                    <strong>{t(`learnerPet.types.${petType}`)}</strong>
                    <small>
                      {learnerPetEnabled && learnerPetType === petType
                        ? t('learnerPet.selectedBadge')
                        : t('learnerPet.selectAction')}
                    </small>
                    <span className="profile-pet-settings__check" aria-hidden="true">✓</span>
                  </label>
                ))}
              </div>
              <p className="profile-pet-settings__reminder">{t('learnerPet.enableHint')}</p>
            </fieldset>
          ) : null}

          <button className="button button--primary auth-form__submit" type="submit" disabled={isSaving}>
            {isSaving ? t('auth.actions.saving') : t('auth.actions.saveChanges')}
          </button>
        </form>
      </Container>
    </div>
  )
}

export default ProfilePage
