import { useTranslation } from 'react-i18next'
import {
  persistLanguage,
  resolveLanguage,
  type SupportedLanguage,
} from '../i18n'
import './LanguageSwitcher.css'

function LanguageSwitcher() {
  const { i18n, t } = useTranslation()
  const currentLanguage = resolveLanguage(i18n.resolvedLanguage ?? i18n.language)

  function changeLanguage(language: SupportedLanguage) {
    persistLanguage(language)
    void i18n.changeLanguage(language)
  }

  return (
    <div className="language-switcher" aria-label={t('language.label')}>
      <button
        type="button"
        aria-pressed={currentLanguage === 'ar'}
        onClick={() => changeLanguage('ar')}
      >
        {t('language.arabic')}
      </button>
      <button
        type="button"
        aria-pressed={currentLanguage === 'en'}
        onClick={() => changeLanguage('en')}
      >
        {t('language.english')}
      </button>
    </div>
  )
}

export default LanguageSwitcher
