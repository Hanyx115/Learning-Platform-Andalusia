import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import { createAnnouncement, getNotificationTemplates, updateNotificationTemplate, type NotificationTemplate } from '../../api/notifications'
import './cms.css'

function CmsNotificationsPage() {
  const { t } = useTranslation()
  const [templates, setTemplates] = useState<NotificationTemplate[] | null>(null)
  const [editing, setEditing] = useState<NotificationTemplate | null>(null)
  const [recipients, setRecipients] = useState('')
  const [announcement, setAnnouncement] = useState({ titleEn: '', titleAr: '', messageEn: '', messageAr: '', linkUrl: '' })
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const load = useCallback(() => {
    getNotificationTemplates().then(setTemplates).catch((requestError: unknown) => setError(getApiErrorMessage(requestError, t('cmsNotifications.errors.load'))))
  }, [t])
  useEffect(() => {
    let isCurrent = true
    getNotificationTemplates()
      .then((loaded) => { if (isCurrent) setTemplates(loaded) })
      .catch((requestError: unknown) => {
        if (isCurrent) setError(getApiErrorMessage(requestError, t('cmsNotifications.errors.load')))
      })
    return () => { isCurrent = false }
  }, [t])

  async function saveTemplate(event: FormEvent) {
    event.preventDefault()
    if (!editing) return
    setError('')
    try {
      await updateNotificationTemplate(editing.id, editing)
      setEditing(null)
      setMessage(t('cmsNotifications.saved'))
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('cmsNotifications.errors.save')))
    }
  }

  async function sendAnnouncement(event: FormEvent) {
    event.preventDefault()
    setError('')
    try {
      await createAnnouncement({
        recipientUserIds: recipients.split(',').map((value) => value.trim()).filter(Boolean),
        ...announcement,
        linkUrl: announcement.linkUrl || null,
      })
      setRecipients('')
      setAnnouncement({ titleEn: '', titleAr: '', messageEn: '', messageAr: '', linkUrl: '' })
      setMessage(t('cmsNotifications.queued'))
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('cmsNotifications.errors.send')))
    }
  }

  return (
    <section className="dashboard-section cms-page">
      <header className="dashboard-section__heading"><h1>{t('cmsNotifications.title')}</h1><p>{t('cmsNotifications.description')}</p></header>
      {message ? <p role="status" className="form-success">{message}</p> : null}
      {error ? <p role="alert" className="form-error">{error}</p> : null}

      <form className="dashboard-card cms-form" onSubmit={sendAnnouncement}>
        <h2>{t('cmsNotifications.announcementTitle')}</h2>
        <label>{t('cmsNotifications.recipients')}<input required value={recipients} onChange={(event) => setRecipients(event.target.value)} /></label>
        <div className="cms-form__grid">
          <label>{t('cms.fields.titleEn')}<input required value={announcement.titleEn} onChange={(event) => setAnnouncement({ ...announcement, titleEn: event.target.value })} /></label>
          <label>{t('cms.fields.titleAr')}<input required dir="rtl" value={announcement.titleAr} onChange={(event) => setAnnouncement({ ...announcement, titleAr: event.target.value })} /></label>
          <label className="cms-form__wide">{t('cms.fields.bodyEn')}<textarea required rows={3} value={announcement.messageEn} onChange={(event) => setAnnouncement({ ...announcement, messageEn: event.target.value })} /></label>
          <label className="cms-form__wide">{t('cms.fields.bodyAr')}<textarea required rows={3} dir="rtl" value={announcement.messageAr} onChange={(event) => setAnnouncement({ ...announcement, messageAr: event.target.value })} /></label>
          <label>{t('cms.fields.linkUrl')}<input type="url" value={announcement.linkUrl} onChange={(event) => setAnnouncement({ ...announcement, linkUrl: event.target.value })} /></label>
        </div>
        <button className="button button--primary" type="submit">{t('cmsNotifications.send')}</button>
      </form>

      {editing ? (
        <form className="dashboard-card cms-form" onSubmit={saveTemplate}>
          <h2>{editing.code}</h2>
          <div className="cms-form__grid">
            <label>{t('cms.fields.titleEn')}<input required value={editing.titleEn} onChange={(event) => setEditing({ ...editing, titleEn: event.target.value })} /></label>
            <label>{t('cms.fields.titleAr')}<input required dir="rtl" value={editing.titleAr} onChange={(event) => setEditing({ ...editing, titleAr: event.target.value })} /></label>
            <label className="cms-form__wide">{t('cms.fields.bodyEn')}<textarea required rows={3} value={editing.messageEn} onChange={(event) => setEditing({ ...editing, messageEn: event.target.value })} /></label>
            <label className="cms-form__wide">{t('cms.fields.bodyAr')}<textarea required rows={3} dir="rtl" value={editing.messageAr} onChange={(event) => setEditing({ ...editing, messageAr: event.target.value })} /></label>
            <label className="cms-checkbox"><input type="checkbox" checked={editing.isActive} onChange={(event) => setEditing({ ...editing, isActive: event.target.checked })} /> {t('cmsNotifications.active')}</label>
          </div>
          <div className="dashboard-card__actions"><button className="button button--primary" type="submit">{t('cms.save')}</button><button className="button button--secondary" type="button" onClick={() => setEditing(null)}>{t('cms.cancel')}</button></div>
        </form>
      ) : null}

      {templates === null ? <p>{t('common.loading')}</p> : null}
      <div className="dashboard-list">
        {templates?.map((template) => (
          <article className="dashboard-card" key={template.id}>
            <div className="dashboard-card__heading"><h2>{template.code}</h2><span className={`status-badge ${template.isActive ? 'status-badge--positive' : 'status-badge--neutral'}`}>{template.isActive ? t('cmsNotifications.active') : t('cmsNotifications.inactive')}</span></div>
            <p>{template.titleEn} / {template.titleAr}</p>
            <button className="button button--secondary" type="button" onClick={() => setEditing(template)}>{t('cms.edit')}</button>
          </article>
        ))}
      </div>
    </section>
  )
}

export default CmsNotificationsPage
