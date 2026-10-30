import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import Container from '../../components/Container'
import { useAuthProfile } from '../../auth/profileContext'
import '../profile/profile-dashboard.css'

function CmsLayout() {
  const { t } = useTranslation()
  const { profile } = useAuthProfile()
  const canManageWebsiteContent = profile?.roles.some((role) => role === 'ContentManager' || role === 'Admin')
  return (
    <div className="profile-dashboard">
      <Container className="profile-dashboard__layout">
        <nav className="profile-dashboard__sidebar" aria-label={t('cmsNav.label')}>
          <NavLink to="/cms/catalog" className="profile-dashboard__link">{t('cmsNav.catalog')}</NavLink>
          {canManageWebsiteContent ? (
            <>
              <NavLink to="/cms/content" className="profile-dashboard__link">{t('cmsNav.content')}</NavLink>
              <NavLink to="/cms/notifications" className="profile-dashboard__link">{t('cmsNav.notifications')}</NavLink>
            </>
          ) : null}
        </nav>
        <div className="profile-dashboard__content"><Outlet /></div>
      </Container>
    </div>
  )
}

export default CmsLayout
