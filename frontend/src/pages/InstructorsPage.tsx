import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getPublishedContent, type CmsContentItem } from '../api/cms'
import Card from '../components/Card'
import Container from '../components/Container'
import './PublicPages.css'

function InstructorsPage() {
  const { t, i18n } = useTranslation()
  const [items, setItems] = useState<CmsContentItem[] | null>(null)
  const [failed, setFailed] = useState(false)
  const isArabic = i18n.resolvedLanguage?.startsWith('ar') ?? true

  useEffect(() => {
    let isCurrent = true
    getPublishedContent('InstructorProfile')
      .then((loaded) => { if (isCurrent) setItems(loaded) })
      .catch(() => { if (isCurrent) setFailed(true) })
    return () => { isCurrent = false }
  }, [])

  return (
    <main className="public-page">
      <Container>
        <header className="public-page__header">
          <p className="public-page__eyebrow">{t('instructors.eyebrow')}</p>
          <h1>{t('instructors.title')}</h1>
          <p>{t('instructors.description')}</p>
        </header>
        {items === null && !failed ? <p>{t('common.loading')}</p> : null}
        {failed ? <p role="alert">{t('common.error')}</p> : null}
        {items?.length === 0 ? <p>{t('instructors.empty')}</p> : null}
        <div className="card-grid">
          {items?.map((item) => (
            <Card key={item.id}>
              {item.imageUrl ? <img className="home-card-photo" src={item.imageUrl} alt="" /> : null}
              <h2>{isArabic ? item.titleAr : item.titleEn}</h2>
              <p>{isArabic ? item.bodyAr : item.bodyEn}</p>
            </Card>
          ))}
        </div>
      </Container>
    </main>
  )
}

export default InstructorsPage
