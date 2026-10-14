import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import Container from '../components/Container'
import './auth/auth.css'

function ForbiddenPage() {
  const { t } = useTranslation()

  return (
    <Container className="auth-state-page page">
      <h1>{t('forbidden.title')}</h1>
      <p>{t('forbidden.description')}</p>
      <Link className="button button--primary" to="/">{t('forbidden.returnHome')}</Link>
    </Container>
  )
}

export default ForbiddenPage
