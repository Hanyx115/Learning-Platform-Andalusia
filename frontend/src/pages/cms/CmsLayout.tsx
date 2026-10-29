import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router-dom'
import Container from '../../components/Container'
import '../profile/profile-dashboard.css'

function CmsLayout() {
  const { t } = useTranslation()
  return (
    <div className="profile-dashboard">
      <Container className="profile-dashboard__layout">
        <nav className="profile-dashboard__sidebar" aria-label={t('cmsNav.label')}>
          <NavLink to="/cms/content" className="profile-dashboard__link">{t('cmsNav.content')}</NavLink>
          <NavLink to="/cms/notifications" className="profile-dashboard__link">{t('cmsNav.notifications')}</NavLink>
        </nav>
        <div className="profile-dashboard__content"><Outlet /></div>
      </Container>
    </div>
  )
}

export default CmsLayout
