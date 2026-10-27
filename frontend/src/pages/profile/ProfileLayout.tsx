import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuthProfile } from '../../auth/profileContext'
import Container from '../../components/Container'
import './profile-dashboard.css'

function ProfileLayout() {
  const { t } = useTranslation()
  const { profile } = useAuthProfile()
  const isLearner = profile?.roles.includes('Learner') ?? false

  return (
    <div className="profile-dashboard">
      <Container className="profile-dashboard__layout">
        <nav className="profile-dashboard__sidebar" aria-label={t('profileNav.label')}>
          <NavLink to="/profile" end className="profile-dashboard__link">
            {t('profileNav.profile')}
          </NavLink>
          {isLearner ? (
            <>
              <NavLink to="/profile/applications" className="profile-dashboard__link">
                {t('profileNav.applications')}
              </NavLink>
              <NavLink to="/profile/enrollments" className="profile-dashboard__link">
                {t('profileNav.enrollments')}
              </NavLink>
              <NavLink to="/profile/schedule" className="profile-dashboard__link">
                {t('profileNav.schedule')}
              </NavLink>
              <NavLink to="/profile/attendance" className="profile-dashboard__link">
                {t('profileNav.attendance')}
              </NavLink>
            </>
          ) : null}
        </nav>
        <div className="profile-dashboard__content">
          <Outlet />
        </div>
      </Container>
    </div>
  )
}

export default ProfileLayout
