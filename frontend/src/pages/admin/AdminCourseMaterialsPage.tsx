import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  createCourseMaterial,
  createCourseMaterialFile,
  downloadManagedCourseMaterialFile,
  getCourseMaterialsForManagement,
  updateCourseMaterial,
  updateCourseMaterialMetadata,
  type CourseMaterial,
  type CourseMaterialMetadataRequest,
  type CourseMaterialRequest,
} from '../../api/content'
import Container from '../../components/Container'
import { saveBlob } from '../../utils/download'

const MATERIAL_TYPES = ['Document', 'Video', 'Link', 'Other']
const VISIBILITIES = ['Published', 'Hidden']

interface MaterialDraft {
  sourceType: 'link' | 'file'
  title: string
  description: string
  resourceUrl: string
  materialType: string
  sortOrder: string
  visibility: string
}

const emptyDraft: MaterialDraft = {
  sourceType: 'link',
  title: '',
  description: '',
  resourceUrl: '',
  materialType: 'Document',
  sortOrder: '0',
  visibility: 'Published',
}

function AdminCourseMaterialsPage() {
  const { t } = useTranslation()
  const { courseId } = useParams()
  const isAdminContext = useLocation().pathname.startsWith('/admin')
  const backLink = isAdminContext ? '/admin/catalog' : '/instructor/offerings'

  const [materials, setMaterials] = useState<CourseMaterial[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [draft, setDraft] = useState<MaterialDraft>(emptyDraft)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [saveError, setSaveError] = useState('')

  const load = useCallback(() => {
    if (!courseId) {
      return
    }

    getCourseMaterialsForManagement(courseId)
      .then((loaded) => setMaterials(loaded))
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403 || getApiErrorStatus(requestError) === 404) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminMaterials.errors.load')))
        }
      })
  }, [courseId, t])

  useEffect(() => {
    load()
  }, [load])

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  function startEdit(material: CourseMaterial) {
    setEditingId(material.id)
    setSelectedFile(null)
    setDraft({
      sourceType: material.resourceType === 'File' ? 'file' : 'link',
      title: material.title,
      description: material.description ?? '',
      resourceUrl: material.resourceUrl ?? '',
      materialType: material.materialType,
      sortOrder: material.sortOrder.toString(),
      visibility: material.visibility,
    })
  }

  async function handleSave() {
    if (!courseId) {
      return
    }

    setSaveError('')
    const metadata: CourseMaterialMetadataRequest = {
      title: draft.title,
      description: draft.description.trim() === '' ? null : draft.description,
      materialType: draft.materialType,
      sortOrder: Number(draft.sortOrder),
      visibility: draft.visibility,
    }

    try {
      if (editingId) {
        if (draft.sourceType === 'file') {
          await updateCourseMaterialMetadata(editingId, metadata)
        } else {
          await updateCourseMaterial(editingId, { ...metadata, resourceUrl: draft.resourceUrl })
        }
      } else if (draft.sourceType === 'file') {
        if (!selectedFile) {
          setSaveError(t('adminMaterials.errors.fileRequired'))
          return
        }
        await createCourseMaterialFile(courseId, metadata, selectedFile)
      } else {
        const request: CourseMaterialRequest = { ...metadata, resourceUrl: draft.resourceUrl }
        await createCourseMaterial(courseId, request)
      }
      setDraft(emptyDraft)
      setSelectedFile(null)
      setEditingId(null)
      load()
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('adminMaterials.errors.save')))
    }
  }

  async function handleDownload(material: CourseMaterial) {
    if (!material.originalFileName) {
      return
    }

    setDownloadingId(material.id)
    setSaveError('')
    try {
      saveBlob(await downloadManagedCourseMaterialFile(material.id), material.originalFileName)
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('adminMaterials.errors.download')))
    } finally {
      setDownloadingId(null)
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('adminMaterials.title')}</h1>
        <p>{t('adminMaterials.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to={backLink}>
          {t('adminMaterials.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}

      {materials === null ? (
        <p role="status">{t('adminMaterials.loading')}</p>
      ) : materials.length === 0 ? (
        <p>{t('adminMaterials.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {materials.map((material) => (
            <article key={material.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h3 dir="auto">{material.title}</h3>
                <span className="status-badge status-badge--neutral">
                  {t(`materials.types.${material.materialType}`, material.materialType)}
                </span>
              </div>
              {material.description ? <p dir="auto">{material.description}</p> : null}
              <div className="dashboard-card__meta">
                <span>{material.visibility}</span>
                <span>
                  {material.resourceType === 'File'
                    ? material.originalFileName
                    : material.resourceUrl}
                </span>
              </div>
              <div className="dashboard-card__actions">
                {material.resourceType === 'File' ? (
                  <button
                    className="button button--primary"
                    type="button"
                    disabled={downloadingId === material.id}
                    onClick={() => void handleDownload(material)}
                  >
                    {downloadingId === material.id
                      ? t('adminMaterials.downloading')
                      : t('adminMaterials.downloadFile')}
                  </button>
                ) : material.resourceUrl ? (
                  <a className="button button--primary" href={material.resourceUrl} target="_blank" rel="noreferrer">
                    {t('adminMaterials.openLink')}
                  </a>
                ) : null}
                <button className="button button--secondary" type="button" onClick={() => startEdit(material)}>
                  {t('adminCatalog.edit')}
                </button>
              </div>
            </article>
          ))}
        </div>
      )}

      <form
        className="dashboard-card"
        onSubmit={(event) => {
          event.preventDefault()
          void handleSave()
        }}
      >
        <h2>{editingId ? t('adminMaterials.editMaterial') : t('adminMaterials.newMaterial')}</h2>
        {!editingId ? (
          <fieldset className="material-source-picker">
            <legend>{t('adminMaterials.fields.attachmentType')}</legend>
            <div className="submission-methods">
              <button
                className={`submission-method${draft.sourceType === 'link' ? ' submission-method--active' : ''}`}
                type="button"
                aria-pressed={draft.sourceType === 'link'}
                onClick={() => {
                  setSelectedFile(null)
                  setDraft((previous) => ({ ...previous, sourceType: 'link' }))
                }}
              >
                {t('adminMaterials.linkOption')}
              </button>
              <button
                className={`submission-method${draft.sourceType === 'file' ? ' submission-method--active' : ''}`}
                type="button"
                aria-pressed={draft.sourceType === 'file'}
                onClick={() => setDraft((previous) => ({ ...previous, sourceType: 'file', resourceUrl: '' }))}
              >
                {t('adminMaterials.fileOption')}
              </button>
            </div>
          </fieldset>
        ) : (
          <p className="material-current-source">
            {draft.sourceType === 'file'
              ? t('adminMaterials.editingFile')
              : t('adminMaterials.editingLink')}
          </p>
        )}
        <label>
          {t('adminMaterials.fields.title')}
          <input
            type="text"
            required
            value={draft.title}
            onChange={(event) => setDraft((previous) => ({ ...previous, title: event.target.value }))}
          />
        </label>
        <label>
          {t('adminMaterials.fields.description')}
          <textarea
            rows={3}
            value={draft.description}
            onChange={(event) => setDraft((previous) => ({ ...previous, description: event.target.value }))}
          />
        </label>
        {draft.sourceType === 'link' ? (
          <label>
            {t('adminMaterials.fields.resourceUrl')}
            <input
              type="url"
              required
              value={draft.resourceUrl}
              onChange={(event) => setDraft((previous) => ({ ...previous, resourceUrl: event.target.value }))}
            />
          </label>
        ) : !editingId ? (
          <label className="submission-file-picker">
            <span>{t('adminMaterials.fields.file')}</span>
            <input
              type="file"
              required
              accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.txt,.csv,.png,.jpg,.jpeg,.webp,.zip"
              onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
            />
            <small>{t('adminMaterials.fileHelp')}</small>
          </label>
        ) : null}
        <label>
          {t('adminMaterials.fields.materialType')}
          <select
            value={draft.materialType}
            onChange={(event) => setDraft((previous) => ({ ...previous, materialType: event.target.value }))}
          >
            {MATERIAL_TYPES.map((type) => (
              <option key={type} value={type}>
                {t(`materials.types.${type}`, type)}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t('adminMaterials.fields.visibility')}
          <select
            value={draft.visibility}
            onChange={(event) => setDraft((previous) => ({ ...previous, visibility: event.target.value }))}
          >
            {VISIBILITIES.map((visibility) => (
              <option key={visibility} value={visibility}>
                {visibility}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t('adminMaterials.fields.sortOrder')}
          <input
            type="number"
            min={0}
            step="1"
            required
            value={draft.sortOrder}
            onChange={(event) => setDraft((previous) => ({ ...previous, sortOrder: event.target.value }))}
          />
        </label>
        <div className="dashboard-card__actions">
          <button className="button button--primary" type="submit">
            {editingId ? t('adminCatalog.save') : t('adminCatalog.create')}
          </button>
          {editingId ? (
            <button
              className="button button--secondary"
              type="button"
              onClick={() => {
                setEditingId(null)
                setDraft(emptyDraft)
                setSelectedFile(null)
              }}
            >
              {t('adminCatalog.cancel')}
            </button>
          ) : null}
        </div>
      </form>
    </section>
  )
}

export default AdminCourseMaterialsPage
