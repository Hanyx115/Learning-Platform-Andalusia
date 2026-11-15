import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getFaqs, type CmsContentItem } from '../api/cms'
import Container from '../components/Container'
import './PublicPages.css'

function FaqPage() {
  const { t, i18n } = useTranslation()
  const [items, setItems] = useState<CmsContentItem[] | null>(null)
  const [failed, setFailed] = useState(false)
  const isArabic = i18n.resolvedLanguage?.startsWith('ar') ?? true

  useEffect(() => {
    let isCurrent = true
    getFaqs()
      .then((loaded) => { if (isCurrent) setItems(loaded) })
      .catch(() => { if (isCurrent) setFailed(true) })
    return () => { isCurrent = false }
  }, [])

  return (
    <main className="public-page">
      <Container>
        <header className="public-page__header">
          <p className="public-page__eyebrow">{t('faq.eyebrow')}</p>
          <h1>{t('faq.title')}</h1>
          <p>{t('faq.description')}</p>
        </header>
        {items === null && !failed ? <p>{t('common.loading')}</p> : null}
        {failed ? <p role="alert">{t('common.error')}</p> : null}
        {items?.length === 0 ? <p>{t('faq.empty')}</p> : null}
        <div className="faq-list">
          {items?.map((item) => (
            <details key={item.id} className="faq-item">
              <summary>{isArabic ? item.titleAr : item.titleEn}</summary>
              <p>{isArabic ? item.bodyAr : item.bodyEn}</p>
            </details>
          ))}
        </div>
      </Container>
    </main>
  )
}

export default FaqPage
