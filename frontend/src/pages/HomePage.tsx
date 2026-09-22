import { useTranslation } from 'react-i18next'

function HomePage() {
  const { t } = useTranslation()

  return (
    <>
      <h1>{t('app.foundationTitle')}</h1>
      <p>{t('app.foundationDescription')}</p>
    </>
  )
}

export default HomePage
