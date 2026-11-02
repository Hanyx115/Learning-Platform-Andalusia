import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import Container from '../../components/Container'
import WorkspaceNavigation from '../../components/WorkspaceNavigation'
import { useAuthProfile } from '../../auth/profileContext'
import '../profile/profile-dashboard.css'

function AdminLayout() {
  const { t } = useTranslation()
  const { profile } = useAuthProfile()
  const isAdmin = profile?.roles.includes('Admin') ?? false

  return (
    <div className="profile-dashboard">
      <Container className="profile-dashboard__layout">
        <nav className="profile-dashboard__sidebar" aria-label={t('adminNav.label')}>
          <WorkspaceNavigation title={t('adminNav.label')} />
          <NavLink to="/admin/catalog" className="profile-dashboard__link">
            {t('adminNav.catalog')}
          </NavLink>
          <NavLink to="/admin/locations" className="profile-dashboard__link">
            {t('adminNav.locations')}
          </NavLink>
          <NavLink to="/admin/students" className="profile-dashboard__link">
            {t('adminNav.students')}
          </NavLink>
          <NavLink to="/admin/instructors" className="profile-dashboard__link">
            {t('adminNav.instructors')}
          </NavLink>
          <NavLink to="/admin/payments" className="profile-dashboard__link">
            {t('adminNav.payments')}
          </NavLink>
          {isAdmin ? (
            <NavLink to="/admin/users" className="profile-dashboard__link">
              {t('adminNav.users')}
            </NavLink>
          ) : null}
        </nav>
        <div className="profile-dashboard__content">
          <Outlet />
        </div>
      </Container>
    </div>
  )
}

export default AdminLayout
