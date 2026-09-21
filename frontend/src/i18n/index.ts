import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import { arTranslations } from './locales/ar'
import { enTranslations } from './locales/en'

export const supportedLanguages = ['ar', 'en'] as const
export type SupportedLanguage = (typeof supportedLanguages)[number]

const languageStorageKey = 'andalusia-academy-language'
const storedLanguage = localStorage.getItem(languageStorageKey)
const initialLanguage: SupportedLanguage = storedLanguage === 'en' ? 'en' : 'ar'

void i18n.use(initReactI18next).init({
  resources: {
    ar: { translation: arTranslations },
    en: { translation: enTranslations },
  },
  lng: initialLanguage,
  fallbackLng: 'ar',
  supportedLngs: supportedLanguages,
  interpolation: {
    escapeValue: false,
  },
})

export function persistLanguage(language: SupportedLanguage) {
  localStorage.setItem(languageStorageKey, language)
}

export function resolveLanguage(language: string): SupportedLanguage {
  return language.startsWith('en') ? 'en' : 'ar'
}

export default i18n
