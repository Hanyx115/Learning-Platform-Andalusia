import { useTranslation } from 'react-i18next'
import Container from '../components/Container'

interface PlaceholderPageProps {
  titleKey: string
  descriptionKey: string
}

function PlaceholderPage({ titleKey, descriptionKey }: PlaceholderPageProps) {
  const { t } = useTranslation()

  return (
    <Container className="page">
      <h1>{t(titleKey)}</h1>
      <p>{t(descriptionKey)}</p>
    </Container>
  )
}

export default PlaceholderPage
