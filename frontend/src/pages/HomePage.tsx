import { useTranslation } from 'react-i18next'

function HomePage() {
  const { t } = useTranslation()

  return (
    <main>
      <h1>{t('app.foundationTitle')}</h1>
      <p>{t('app.foundationDescription')}</p>
    </main>
  )
}

export default HomePage
