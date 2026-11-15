import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage, getProfileByUserId, type Profile } from '../../api/auth'
import Container from '../../components/Container'
import '../profile/profile-dashboard.css'

function StaffApplicantProfilePage() {
  const { t } = useTranslation()
  const { userId } = useParams()
  const [profile, setProfile] = useState<Profile | null>(null)
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    if (!userId) return

    let isCurrent = true
    getProfileByUserId(userId)
      .then((loadedProfile) => {
        if (isCurrent) setProfile(loadedProfile)
      })
      .catch((requestError: unknown) => {
        if (isCurrent) {
          setLoadError(getApiErrorMessage(requestError, t('staffApplicantProfile.errors.load')))
        }
      })

    return () => { isCurrent = false }
  }, [t, userId])

  const displayedError = userId ? loadError : t('staffApplicantProfile.errors.notFound')
  const fullName = profile ? `${profile.firstName} ${profile.lastName}`.trim() : ''
  const initials = profile ? `${profile.firstName.charAt(0)}${profile.lastName.charAt(0)}`.toUpperCase() : ''

  return (
    <div className="profile-dashboard">
      <Container>
        <section className="dashboard-section applicant-profile-page">
          <Link className="button button--secondary applicant-profile-page__back" to="/staff/applications">
            {t('staffApplicantProfile.back')}
          </Link>

          <div className="dashboard-section__heading">
            <span className="applicant-profile-page__eyebrow">{t('staffApplicantProfile.eyebrow')}</span>
            <h1>{t('staffApplicantProfile.title')}</h1>
            <p>{t('staffApplicantProfile.description')}</p>
          </div>

          {displayedError ? <p className="auth-message auth-message--error" role="alert">{displayedError}</p> : null}

          {!profile && !displayedError ? (
            <p role="status">{t('staffApplicantProfile.loading')}</p>
          ) : profile ? (
            <article className="dashboard-card applicant-profile-card">
              <div className="applicant-profile-card__identity">
                <span className="applicant-profile-card__avatar" aria-hidden="true">{initials}</span>
                <div>
                  <h2 dir="auto">{fullName}</h2>
                  <a href={`mailto:${profile.email}`}>{profile.email}</a>
                </div>
                <span className="status-badge status-badge--neutral">{t('staffApplicantProfile.readOnly')}</span>
              </div>

              <dl className="applicant-profile-details">
                <div>
                  <dt>{t('staffApplicantProfile.fields.fullName')}</dt>
                  <dd dir="auto">{fullName}</dd>
                </div>
                <div>
                  <dt>{t('staffApplicantProfile.fields.email')}</dt>
                  <dd><a href={`mailto:${profile.email}`}>{profile.email}</a></dd>
                </div>
                <div>
                  <dt>{t('staffApplicantProfile.fields.phone')}</dt>
                  <dd dir="ltr"><a href={`tel:${profile.phoneNumber}`}>{profile.phoneNumber}</a></dd>
                </div>
                <div>
                  <dt>{t('staffApplicantProfile.fields.country')}</dt>
                  <dd>{profile.countryCode}</dd>
                </div>
                <div>
                  <dt>{t('staffApplicantProfile.fields.language')}</dt>
                  <dd>{t(`staffApplicantProfile.languages.${profile.preferredLanguage}`, { defaultValue: profile.preferredLanguage })}</dd>
                </div>
                <div>
                  <dt>{t('staffApplicantProfile.fields.roles')}</dt>
                  <dd className="applicant-profile-details__roles">
                    {profile.roles.map((role) => (
                      <span className="status-badge status-badge--neutral" key={role}>
                        {t(`profile.roleNames.${role}`, { defaultValue: role })}
                      </span>
                    ))}
                  </dd>
                </div>
              </dl>

              <p className="applicant-profile-card__notice">{t('staffApplicantProfile.notice')}</p>
            </article>
          ) : null}
        </section>
      </Container>
    </div>
  )
}

export default StaffApplicantProfilePage
