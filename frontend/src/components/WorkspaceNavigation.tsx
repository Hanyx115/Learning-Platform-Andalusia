import { useTranslation } from 'react-i18next'
import { useAuthProfile } from '../auth/profileContext'

interface WorkspaceNavigationProps {
  title: string
}

function WorkspaceNavigation({ title }: WorkspaceNavigationProps) {
  const { t } = useTranslation()
  const { profile } = useAuthProfile()
  const displayName = profile ? `${profile.firstName} ${profile.lastName}`.trim() : t('app.name')
  const initials = profile
    ? `${profile.firstName.charAt(0)}${profile.lastName.charAt(0)}`.toUpperCase()
    : 'AA'
  const roles = profile?.roles
    .map((role) => t(`profile.roleNames.${role}`, role))
    .join(', ')

  return (
    <>
      <div className="profile-dashboard__sidebar-header">
        <span>{t('workspace.navigation')}</span>
        <strong>{title}</strong>
      </div>
      <div className="profile-dashboard__sidebar-separator" aria-hidden="true" />
      <div className="profile-dashboard__identity">
        <span className="profile-dashboard__avatar" aria-hidden="true">{initials}</span>
        <span className="profile-dashboard__identity-copy">
          <strong dir="auto">{displayName}</strong>
          {roles ? <small>{roles}</small> : null}
        </span>
      </div>
    </>
  )
}

export default WorkspaceNavigation
