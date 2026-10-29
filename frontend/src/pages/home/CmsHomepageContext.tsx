import { useEffect, useState, type ReactNode } from 'react'
import { getPublishedContent, type CmsContentItem } from '../../api/cms'
import { HomepageContentContext } from './CmsHomepageContent'

export function CmsHomepageProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CmsContentItem[]>([])

  useEffect(() => {
    let isCurrent = true
    getPublishedContent()
      .then((loaded) => {
        if (isCurrent) setItems(loaded)
      })
      .catch(() => {
        if (isCurrent) setItems([])
      })
    return () => { isCurrent = false }
  }, [])

  return <HomepageContentContext.Provider value={items}>{children}</HomepageContentContext.Provider>
}
