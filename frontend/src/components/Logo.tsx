import { useTranslation } from 'react-i18next'
import logoSrc from '../assets/andalusia-academy-logo.svg'

function Logo() {
  const { t } = useTranslation()

  return <img src={logoSrc} alt={t('app.name')} />
}

export default Logo
