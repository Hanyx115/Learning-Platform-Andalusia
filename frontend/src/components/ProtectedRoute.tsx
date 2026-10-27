import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuthProfile } from '../auth/profileContext'
import { useAuthSession } from '../auth/session'

interface ProtectedRouteProps {
  allowedRoles?: string[]
}

function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { t } = useTranslation()
  const tokens = useAuthSession()
  const { profile, isLoading } = useAuthProfile()
  const location = useLocation()

  if (!tokens) {
    const returnPath = `${location.pathname}${location.search}${location.hash}`
    return <Navigate to="/login" replace state={{ from: returnPath }} />
  }

  if (allowedRoles?.length) {
    if (isLoading) {
      return <p className="auth-state-page" role="status">{t('profile.loading')}</p>
    }

    if (!profile || !allowedRoles.some((role) => profile.roles.includes(role))) {
      return <Navigate to="/forbidden" replace />
    }
  }

  return <Outlet />
}

export default ProtectedRoute
