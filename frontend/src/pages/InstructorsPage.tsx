import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getPublishedContent, type CmsContentItem } from '../api/cms'
import Container from '../components/Container'
import './PublicPages.css'

function getInitials(name: string) {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

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
    <main className="public-page team-page">
      <Container>
        <header className="public-page__header">
          <p className="public-page__eyebrow">{t('instructors.eyebrow')}</p>
          <h1>{t('instructors.title')}</h1>
          <p>{t('instructors.description')}</p>
        </header>
        {items === null && !failed ? <p>{t('common.loading')}</p> : null}
        {failed ? <p role="alert">{t('common.error')}</p> : null}
        {items?.length === 0 ? <p>{t('instructors.empty')}</p> : null}
        <div className="team-grid">
          {items?.map((item) => {
            const name = isArabic ? item.titleAr : item.titleEn
            const biography = isArabic ? item.bodyAr : item.bodyEn
            const isLinkedIn = item.linkUrl?.toLowerCase().includes('linkedin.com') ?? false

            return (
              <article className="team-card" key={item.id}>
                {item.imageUrl ? (
                  <img className="team-card__photo" src={item.imageUrl} alt={t('instructors.photoAlt', { name })} />
                ) : (
                  <div className="team-card__avatar" aria-hidden="true">{getInitials(name)}</div>
                )}
                <div className="team-card__content">
                  <p className="team-card__role">{t('instructors.role')}</p>
                  <h2 dir="auto">{name}</h2>
                  <p>{biography}</p>
                  {item.linkUrl ? (
                    <a className="team-card__link" href={item.linkUrl} target="_blank" rel="noreferrer">
                      {isLinkedIn ? t('instructors.linkedin') : t('instructors.professionalProfile')}
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                        <path d="M14 5h5v5M10 14 19 5M19 13v6H5V5h6" strokeLinecap="round" strokeLinejoin="round" />
                      </svg>
                    </a>
                  ) : null}
                </div>
              </article>
            )
          })}
        </div>
      </Container>
    </main>
  )
}

export default InstructorsPage
