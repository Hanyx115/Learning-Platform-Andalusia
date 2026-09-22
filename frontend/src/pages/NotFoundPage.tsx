import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import Container from '../components/Container'

function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <Container className="page">
      <h1>{t('notFound.title')}</h1>
      <p>{t('notFound.description')}</p>
      <Link to="/">{t('notFound.returnHome')}</Link>
    </Container>
  )
}

export default NotFoundPage
