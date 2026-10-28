import { createContext, useContext, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type { CmsContentItem, CmsContentType } from '../../api/cms'

export const HomepageContentContext = createContext<CmsContentItem[]>([])

export function useCmsItem(contentType: CmsContentType, key: string) {
  return useContext(HomepageContentContext).find(
    (item) => item.contentType === contentType && item.key === key,
  )
}

export function useCmsItems(contentType: CmsContentType) {
  const items = useContext(HomepageContentContext)
  return useMemo(
    () => items.filter((item) => item.contentType === contentType)
      .sort((left, right) => left.displayOrder - right.displayOrder),
    [items, contentType],
  )
}

export function useLocalizedCmsItem(contentType: CmsContentType, key: string) {
  const { i18n } = useTranslation()
  const item = useCmsItem(contentType, key)
  const isArabic = i18n.resolvedLanguage?.startsWith('ar') ?? true
  return item ? {
    ...item,
    title: isArabic ? item.titleAr : item.titleEn,
    body: isArabic ? item.bodyAr : item.bodyEn,
  } : null
}

export function localizeCmsItem(item: CmsContentItem, language: string | undefined) {
  const isArabic = language?.startsWith('ar') ?? true
  return {
    ...item,
    title: isArabic ? item.titleAr : item.titleEn,
    body: isArabic ? item.bodyAr : item.bodyEn,
  }
}
