import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import {
  createCmsContent,
  deleteCmsContent,
  getCmsContent,
  setCmsPublication,
  updateCmsContent,
  type CmsContentItem,
  type CmsContentRequest,
  type CmsContentType,
} from '../../api/cms'
import './cms.css'

const contentTypes: CmsContentType[] = [
  'HomepageSection', 'PublicPage', 'Faq', 'Partner', 'Testimonial', 'InstructorProfile',
]

const emptyRequest: CmsContentRequest = {
  contentType: 'HomepageSection',
  key: '',
  titleEn: '',
  titleAr: '',
  bodyEn: '',
  bodyAr: '',
  imageUrl: '',
  linkUrl: '',
  relatedUserId: null,
  displayOrder: 0,
  isPublished: false,
}

function CmsContentPage() {
  const { t } = useTranslation()
  const [filter, setFilter] = useState<CmsContentType | ''>('')
  const [items, setItems] = useState<CmsContentItem[] | null>(null)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<CmsContentRequest>(emptyRequest)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const load = useCallback(() => {
    setError('')
    getCmsContent(filter || undefined)
      .then(setItems)
      .catch((requestError: unknown) => setError(getApiErrorMessage(requestError, t('cms.errors.load'))))
  }, [filter, t])

  useEffect(() => {
    let isCurrent = true
    getCmsContent(filter || undefined)
      .then((loaded) => { if (isCurrent) setItems(loaded) })
      .catch((requestError: unknown) => {
        if (isCurrent) setError(getApiErrorMessage(requestError, t('cms.errors.load')))
      })
    return () => { isCurrent = false }
  }, [filter, t])

  function edit(item: CmsContentItem) {
    setEditingId(item.id)
    setForm({
      contentType: item.contentType,
      key: item.key,
      titleEn: item.titleEn,
      titleAr: item.titleAr,
      bodyEn: item.bodyEn,
      bodyAr: item.bodyAr,
      imageUrl: item.imageUrl ?? '',
      linkUrl: item.linkUrl ?? '',
      relatedUserId: item.relatedUserId,
      displayOrder: item.displayOrder,
      isPublished: item.isPublished,
    })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function reset() {
    setEditingId(null)
    setForm({ ...emptyRequest, contentType: filter || 'HomepageSection' })
    setError('')
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      if (editingId) await updateCmsContent(editingId, form)
      else await createCmsContent(form)
      reset()
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('cms.errors.save')))
    } finally {
      setSaving(false)
    }
  }

  async function toggle(item: CmsContentItem) {
    try {
      await setCmsPublication(item.id, !item.isPublished)
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('cms.errors.save')))
    }
  }

  async function remove(item: CmsContentItem) {
    if (!window.confirm(t('cms.confirmDelete', { name: item.key }))) return
    try {
      await deleteCmsContent(item.id)
      if (editingId === item.id) reset()
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('cms.errors.delete')))
    }
  }

  return (
    <section className="dashboard-section cms-page">
      <header className="dashboard-section__heading">
        <h1>{t('cms.title')}</h1>
        <p>{t('cms.description')}</p>
      </header>

      <form className="dashboard-card cms-form" onSubmit={save}>
        <h2>{t(editingId ? 'cms.editTitle' : 'cms.createTitle')}</h2>
        <div className="cms-form__grid">
          <label>{t('cms.fields.type')}
            <select value={form.contentType} onChange={(event) => setForm({ ...form, contentType: event.target.value as CmsContentType })}>
              {contentTypes.map((type) => <option key={type} value={type}>{t(`cms.types.${type}`)}</option>)}
            </select>
          </label>
          <label>{t('cms.fields.key')}
            <input required pattern="[a-z0-9]+(?:-[a-z0-9]+)*" value={form.key} onChange={(event) => setForm({ ...form, key: event.target.value })} />
          </label>
          <label>{t('cms.fields.order')}
            <input required type="number" min="0" value={form.displayOrder} onChange={(event) => setForm({ ...form, displayOrder: Number(event.target.value) })} />
          </label>
          <label>{t('cms.fields.relatedUser')}
            <input value={form.relatedUserId ?? ''} onChange={(event) => setForm({ ...form, relatedUserId: event.target.value || null })} />
          </label>
          <label>{t('cms.fields.titleEn')}
            <input required value={form.titleEn} onChange={(event) => setForm({ ...form, titleEn: event.target.value })} />
          </label>
          <label>{t('cms.fields.titleAr')}
            <input required dir="rtl" value={form.titleAr} onChange={(event) => setForm({ ...form, titleAr: event.target.value })} />
          </label>
          <label className="cms-form__wide">{t('cms.fields.bodyEn')}
            <textarea required rows={4} value={form.bodyEn} onChange={(event) => setForm({ ...form, bodyEn: event.target.value })} />
          </label>
          <label className="cms-form__wide">{t('cms.fields.bodyAr')}
            <textarea required rows={4} dir="rtl" value={form.bodyAr} onChange={(event) => setForm({ ...form, bodyAr: event.target.value })} />
          </label>
          <label>{t('cms.fields.imageUrl')}
            <input type="url" value={form.imageUrl ?? ''} onChange={(event) => setForm({ ...form, imageUrl: event.target.value })} />
          </label>
          <label>{t('cms.fields.linkUrl')}
            <input type="url" value={form.linkUrl ?? ''} onChange={(event) => setForm({ ...form, linkUrl: event.target.value })} />
          </label>
          <label className="cms-checkbox"><input type="checkbox" checked={form.isPublished} onChange={(event) => setForm({ ...form, isPublished: event.target.checked })} /> {t('cms.fields.published')}</label>
        </div>
        {error ? <p role="alert" className="form-error">{error}</p> : null}
        <div className="dashboard-card__actions">
          <button className="button button--primary" type="submit" disabled={saving}>{saving ? t('cms.saving') : t('cms.save')}</button>
          {editingId ? <button className="button button--secondary" type="button" onClick={reset}>{t('cms.cancel')}</button> : null}
        </div>
      </form>

      <label className="cms-filter">{t('cms.filter')}
        <select value={filter} onChange={(event) => setFilter(event.target.value as CmsContentType | '')}>
          <option value="">{t('cms.allTypes')}</option>
          {contentTypes.map((type) => <option key={type} value={type}>{t(`cms.types.${type}`)}</option>)}
        </select>
      </label>

      {items === null ? <p>{t('common.loading')}</p> : null}
      <div className="dashboard-list">
        {items?.map((item) => (
          <article className="dashboard-card" key={item.id}>
            <div className="dashboard-card__heading">
              <div><h2>{item.titleEn}</h2><p>{item.titleAr}</p></div>
              <span className={`status-badge ${item.isPublished ? 'status-badge--positive' : 'status-badge--neutral'}`}>{item.isPublished ? t('cms.published') : t('cms.draft')}</span>
            </div>
            <div className="dashboard-card__meta"><span>{item.contentType}</span><span>{item.key}</span><span>#{item.displayOrder}</span></div>
            <div className="dashboard-card__actions">
              <button className="button button--secondary" type="button" onClick={() => edit(item)}>{t('cms.edit')}</button>
              <button className="button button--secondary" type="button" onClick={() => toggle(item)}>{t(item.isPublished ? 'cms.unpublish' : 'cms.publish')}</button>
              <button className="button button--secondary" type="button" onClick={() => remove(item)}>{t('cms.delete')}</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

export default CmsContentPage
