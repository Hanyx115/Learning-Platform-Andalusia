import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuthProfile } from '../../auth/profileContext'
import Container from '../../components/Container'
import WorkspaceNavigation from '../../components/WorkspaceNavigation'
import { getWorkspaceNavGroups } from '../../navigation'
import './profile-dashboard.css'

function ProfileLayout() {
  const { t } = useTranslation()
  const { profile } = useAuthProfile()
  const navigationGroups = getWorkspaceNavGroups(profile?.roles ?? [])

  return (
    <div className="profile-dashboard">
      <Container className="profile-dashboard__layout">
        <nav className="profile-dashboard__sidebar" aria-label={t('profileNav.label')}>
          <WorkspaceNavigation title={t('profileNav.label')} />
          {navigationGroups.map((group, groupIndex) => (
            <div className="profile-dashboard__nav-group" key={group.labelKey}>
              {groupIndex > 0 ? (
                <p className="profile-dashboard__nav-heading">{t(group.labelKey)}</p>
              ) : null}
              {group.items.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path}
                  end={item.end}
                  className="profile-dashboard__link"
                >
                  {t(item.labelKey)}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
        <div className="profile-dashboard__content">
          <Outlet />
        </div>
      </Container>
    </div>
  )
}

export default ProfileLayout
