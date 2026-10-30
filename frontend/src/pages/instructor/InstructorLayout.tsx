import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import Container from '../../components/Container'
import WorkspaceNavigation from '../../components/WorkspaceNavigation'
import '../profile/profile-dashboard.css'

function InstructorLayout() {
  const { t } = useTranslation()

  return (
    <div className="profile-dashboard">
      <Container className="profile-dashboard__layout">
        <nav className="profile-dashboard__sidebar" aria-label={t('instructorNav.label')}>
          <WorkspaceNavigation title={t('instructorNav.label')} />
          <NavLink to="/instructor/sessions" className="profile-dashboard__link">
            {t('instructorNav.sessions')}
          </NavLink>
          <NavLink to="/instructor/offerings" className="profile-dashboard__link">
            {t('instructorNav.offerings')}
          </NavLink>
          <NavLink to="/instructor/students" className="profile-dashboard__link">
            {t('instructorNav.students')}
          </NavLink>
        </nav>
        <div className="profile-dashboard__content">
          <Outlet />
        </div>
      </Container>
    </div>
  )
}

export default InstructorLayout
