import { useTranslation } from 'react-i18next'

interface PlaceholderPageProps {
  titleKey: string
  descriptionKey: string
}

function PlaceholderPage({ titleKey, descriptionKey }: PlaceholderPageProps) {
  const { t } = useTranslation()

  return (
    <>
      <h1>{t(titleKey)}</h1>
      <p>{t(descriptionKey)}</p>
    </>
  )
}

export default PlaceholderPage
