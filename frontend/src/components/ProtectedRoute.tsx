import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthSession } from '../auth/session'

function ProtectedRoute() {
  const tokens = useAuthSession()
  const location = useLocation()

  if (!tokens) {
    const returnPath = `${location.pathname}${location.search}${location.hash}`
    return <Navigate to="/login" replace state={{ from: returnPath }} />
  }

  return <Outlet />
}

export default ProtectedRoute
