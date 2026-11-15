import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getNotifications, markAllNotificationsRead, markNotificationRead, type NotificationList } from '../../api/notifications'

function NotificationsPage() {
  const { t, i18n } = useTranslation()
  const [data, setData] = useState<NotificationList | null>(null)
  const [failed, setFailed] = useState(false)
  const isArabic = i18n.resolvedLanguage?.startsWith('ar') ?? true

  useEffect(() => {
    let isCurrent = true
    getNotifications()
      .then((loaded) => { if (isCurrent) setData(loaded) })
      .catch(() => { if (isCurrent) setFailed(true) })
    return () => { isCurrent = false }
  }, [])

  async function markRead(id: string) {
    await markNotificationRead(id)
    setData((current) => current ? {
      unreadCount: Math.max(0, current.unreadCount - 1),
      items: current.items.map((item) => item.id === id ? { ...item, readAtUtc: new Date().toISOString() } : item),
    } : current)
  }

  async function markAll() {
    await markAllNotificationsRead()
    const now = new Date().toISOString()
    setData((current) => current ? {
      unreadCount: 0,
      items: current.items.map((item) => ({ ...item, readAtUtc: item.readAtUtc ?? now })),
    } : current)
  }

  return (
    <section className="dashboard-section">
      <header className="dashboard-section__heading dashboard-card__heading">
        <div>
          <h1>{t('notifications.title')}</h1>
          <p>{t('notifications.description', { count: data?.unreadCount ?? 0 })}</p>
        </div>
        {data && data.unreadCount > 0 ? (
          <button className="button button--secondary" type="button" onClick={markAll}>
            {t('notifications.markAllRead')}
          </button>
        ) : null}
      </header>
      {data === null && !failed ? <p>{t('common.loading')}</p> : null}
      {failed ? <p role="alert">{t('notifications.errors.load')}</p> : null}
      {data?.items.length === 0 ? <p>{t('notifications.empty')}</p> : null}
      <div className="dashboard-list">
        {data?.items.map((item) => (
          <article key={item.id} className={`dashboard-card notification-card${item.readAtUtc ? '' : ' notification-card--unread'}`}>
            <div className="dashboard-card__heading">
              <h2>{isArabic ? item.titleAr : item.titleEn}</h2>
              <time dateTime={item.createdAtUtc}>{new Date(item.createdAtUtc).toLocaleString(i18n.language)}</time>
            </div>
            <p>{isArabic ? item.messageAr : item.messageEn}</p>
            <div className="dashboard-card__actions">
              {item.linkUrl ? <Link className="button button--secondary" to={item.linkUrl}>{t('notifications.open')}</Link> : null}
              {!item.readAtUtc ? (
                <button className="button button--primary" type="button" onClick={() => markRead(item.id)}>
                  {t('notifications.markRead')}
                </button>
              ) : null}
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

export default NotificationsPage
