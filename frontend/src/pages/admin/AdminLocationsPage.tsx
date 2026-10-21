import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  createTrainingLocation,
  getTrainingLocationsForManagement,
  updateTrainingLocation,
  type TrainingLocationRequest,
} from '../../api/admin'
import type { TrainingLocation } from '../../api/catalog'
import Container from '../../components/Container'

interface LocationDraft {
  branchName: string
  roomName: string
  city: string
  address: string
  mapUrl: string
  capacity: string
  isActive: boolean
}

const emptyDraft: LocationDraft = {
  branchName: '',
  roomName: '',
  city: '',
  address: '',
  mapUrl: '',
  capacity: '',
  isActive: true,
}

function AdminLocationsPage() {
  const { t } = useTranslation()
  const [locations, setLocations] = useState<TrainingLocation[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [draft, setDraft] = useState<LocationDraft>(emptyDraft)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saveError, setSaveError] = useState('')

  const load = useCallback(() => {
    getTrainingLocationsForManagement()
      .then(setLocations)
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminLocations.errors.load')))
        }
      })
  }, [t])

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

  function startEdit(location: TrainingLocation) {
    setEditingId(location.id)
    setDraft({
      branchName: location.branchName,
      roomName: location.roomName,
      city: location.city,
      address: location.address,
      mapUrl: location.mapUrl ?? '',
      capacity: location.capacity.toString(),
      isActive: location.isActive,
    })
  }

  async function handleSave() {
    setSaveError('')
    const request: TrainingLocationRequest = {
      branchName: draft.branchName.trim(),
      roomName: draft.roomName.trim(),
      city: draft.city.trim(),
      address: draft.address.trim(),
      mapUrl: draft.mapUrl.trim() || null,
      capacity: Number(draft.capacity),
      isActive: draft.isActive,
    }

    try {
      if (editingId) {
        await updateTrainingLocation(editingId, request)
      } else {
        await createTrainingLocation(request)
      }
      setDraft(emptyDraft)
      setEditingId(null)
      load()
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('adminLocations.errors.save')))
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('adminLocations.title')}</h1>
        <p>{t('adminLocations.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}

      {locations === null ? (
        <p role="status">{t('adminLocations.loading')}</p>
      ) : (
        <div className="dashboard-list">
          {locations.map((location) => (
            <article key={location.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{location.branchName} — {location.roomName}</h2>
                <span className={location.isActive ? 'status-badge status-badge--positive' : 'status-badge status-badge--negative'}>
                  {location.isActive ? t('adminLocations.active') : t('adminLocations.inactive')}
                </span>
              </div>
              <div className="dashboard-card__meta">
                <span>{location.city}</span>
                <span>{location.address}</span>
                <span>{t('adminLocations.capacity', { capacity: location.capacity })}</span>
              </div>
              <div className="dashboard-card__actions">
                <button className="button button--secondary" type="button" onClick={() => startEdit(location)}>
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
        <h2>{editingId ? t('adminLocations.editLocation') : t('adminLocations.newLocation')}</h2>
        <label>
          {t('adminLocations.fields.branchName')}
          <input
            type="text"
            required
            value={draft.branchName}
            onChange={(event) => setDraft((previous) => ({ ...previous, branchName: event.target.value }))}
          />
        </label>
        <label>
          {t('adminLocations.fields.roomName')}
          <input
            type="text"
            required
            value={draft.roomName}
            onChange={(event) => setDraft((previous) => ({ ...previous, roomName: event.target.value }))}
          />
        </label>
        <label>
          {t('adminLocations.fields.city')}
          <input
            type="text"
            required
            value={draft.city}
            onChange={(event) => setDraft((previous) => ({ ...previous, city: event.target.value }))}
          />
        </label>
        <label>
          {t('adminLocations.fields.address')}
          <input
            type="text"
            required
            value={draft.address}
            onChange={(event) => setDraft((previous) => ({ ...previous, address: event.target.value }))}
          />
        </label>
        <label>
          {t('adminLocations.fields.mapUrl')}
          <input
            type="url"
            value={draft.mapUrl}
            onChange={(event) => setDraft((previous) => ({ ...previous, mapUrl: event.target.value }))}
          />
        </label>
        <label>
          {t('adminLocations.fields.capacity')}
          <input
            type="number"
            min={1}
            required
            value={draft.capacity}
            onChange={(event) => setDraft((previous) => ({ ...previous, capacity: event.target.value }))}
          />
        </label>
        {editingId ? (
          <label>
            <input
              type="checkbox"
              checked={draft.isActive}
              onChange={(event) => setDraft((previous) => ({ ...previous, isActive: event.target.checked }))}
            />
            {t('adminLocations.active')}
          </label>
        ) : null}
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

export default AdminLocationsPage
