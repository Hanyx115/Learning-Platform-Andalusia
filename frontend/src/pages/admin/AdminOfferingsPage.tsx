import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  createCourseOffering,
  createProgramOffering,
  getCourseOfferingsForManagement,
  getProgramOfferingsForManagement,
  getTrainingLocationsForManagement,
  updateCourseOffering,
  updateProgramOffering,
  type ManageOfferingRequest,
} from '../../api/admin'
import type { Offering, TrainingLocation } from '../../api/catalog'
import Container from '../../components/Container'

const OFFERING_STATUSES = ['Draft', 'Open', 'Closed', 'Cancelled']

interface OfferingDraft {
  locationId: string
  startsAtUtc: string
  endsAtUtc: string
  applicationOpensAtUtc: string
  applicationClosesAtUtc: string
  price: string
  currency: string
  capacity: string
  status: string
}

const emptyDraft: OfferingDraft = {
  locationId: '',
  startsAtUtc: '',
  endsAtUtc: '',
  applicationOpensAtUtc: '',
  applicationClosesAtUtc: '',
  price: '',
  currency: 'EGP',
  capacity: '',
  status: 'Draft',
}

function toLocalInputValue(isoValue: string) {
  return isoValue ? isoValue.slice(0, 16) : ''
}

function AdminOfferingsPage() {
  const { t } = useTranslation()
  const { courseId, programId } = useParams()
  const parentId = courseId ?? programId ?? ''
  const offeringType = courseId ? 'Course' : 'Program'

  const [offerings, setOfferings] = useState<Offering[] | null>(null)
  const [locations, setLocations] = useState<TrainingLocation[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [draft, setDraft] = useState<OfferingDraft>(emptyDraft)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saveError, setSaveError] = useState('')

  const load = useCallback(() => {
    if (!parentId) {
      return
    }

    const offeringsRequest = offeringType === 'Course'
      ? getCourseOfferingsForManagement(parentId)
      : getProgramOfferingsForManagement(parentId)

    Promise.all([offeringsRequest, getTrainingLocationsForManagement()])
      .then(([loadedOfferings, loadedLocations]) => {
        setOfferings(loadedOfferings)
        setLocations(loadedLocations)
      })
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminOfferings.errors.load')))
        }
      })
  }, [parentId, offeringType, t])

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

  function startEdit(offering: Offering) {
    setEditingId(offering.id)
    setDraft({
      locationId: offering.location?.id ?? '',
      startsAtUtc: toLocalInputValue(offering.startsAtUtc),
      endsAtUtc: toLocalInputValue(offering.endsAtUtc),
      applicationOpensAtUtc: toLocalInputValue(offering.applicationOpensAtUtc),
      applicationClosesAtUtc: toLocalInputValue(offering.applicationClosesAtUtc),
      price: offering.price.toString(),
      currency: offering.currency,
      capacity: offering.capacity.toString(),
      status: offering.status,
    })
  }

  async function handleSave() {
    setSaveError('')
    const request: ManageOfferingRequest = {
      locationId: draft.locationId || null,
      startsAtUtc: new Date(draft.startsAtUtc).toISOString(),
      endsAtUtc: new Date(draft.endsAtUtc).toISOString(),
      applicationOpensAtUtc: new Date(draft.applicationOpensAtUtc).toISOString(),
      applicationClosesAtUtc: new Date(draft.applicationClosesAtUtc).toISOString(),
      price: Number(draft.price),
      currency: draft.currency,
      capacity: Number(draft.capacity),
      status: draft.status,
    }

    try {
      if (editingId) {
        if (offeringType === 'Course') {
          await updateCourseOffering(editingId, request)
        } else {
          await updateProgramOffering(editingId, request)
        }
      } else if (offeringType === 'Course') {
        await createCourseOffering(parentId, request)
      } else {
        await createProgramOffering(parentId, request)
      }
      setDraft(emptyDraft)
      setEditingId(null)
      load()
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('adminOfferings.errors.save')))
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('adminOfferings.title')}</h1>
        <p>{t('adminOfferings.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/admin/catalog">
          {t('adminOfferings.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}

      {offerings === null ? (
        <p role="status">{t('adminOfferings.loading')}</p>
      ) : offerings.length === 0 ? (
        <p>{t('adminOfferings.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {offerings.map((offering) => (
            <article key={offering.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2>
                  {new Date(offering.startsAtUtc).toLocaleDateString()} – {new Date(offering.endsAtUtc).toLocaleDateString()}
                </h2>
                <span className="status-badge status-badge--neutral">{offering.status}</span>
              </div>
              <div className="dashboard-card__meta">
                <span>{offering.price} {offering.currency}</span>
                <span>{t('adminOfferings.capacity', { capacity: offering.capacity })}</span>
                {offering.location ? <span>{offering.location.branchName}</span> : <span>{t('schedule.online')}</span>}
              </div>
              <div className="dashboard-card__actions">
                <button className="button button--secondary" type="button" onClick={() => startEdit(offering)}>
                  {t('adminCatalog.edit')}
                </button>
                <Link
                  className="button button--secondary"
                  to={`/admin/${offeringType === 'Course' ? 'course-offerings' : 'program-offerings'}/${offering.id}/sessions`}
                >
                  {t('adminOfferings.manageSessions')}
                </Link>
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
        <h2>{editingId ? t('adminOfferings.editOffering') : t('adminOfferings.newOffering')}</h2>
        <label>
          {t('adminOfferings.fields.location')}
          <select
            value={draft.locationId}
            onChange={(event) => setDraft((previous) => ({ ...previous, locationId: event.target.value }))}
          >
            <option value="">{t('adminOfferings.fields.online')}</option>
            {locations?.map((location) => (
              <option key={location.id} value={location.id}>
                {location.branchName} — {location.roomName}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t('adminOfferings.fields.startsAt')}
          <input
            type="datetime-local"
            required
            value={draft.startsAtUtc}
            onChange={(event) => setDraft((previous) => ({ ...previous, startsAtUtc: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.endsAt')}
          <input
            type="datetime-local"
            required
            value={draft.endsAtUtc}
            onChange={(event) => setDraft((previous) => ({ ...previous, endsAtUtc: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.applicationOpensAt')}
          <input
            type="datetime-local"
            required
            value={draft.applicationOpensAtUtc}
            onChange={(event) => setDraft((previous) => ({ ...previous, applicationOpensAtUtc: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.applicationClosesAt')}
          <input
            type="datetime-local"
            required
            value={draft.applicationClosesAtUtc}
            onChange={(event) => setDraft((previous) => ({ ...previous, applicationClosesAtUtc: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.price')}
          <input
            type="number"
            min={0}
            step="0.01"
            required
            value={draft.price}
            onChange={(event) => setDraft((previous) => ({ ...previous, price: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.currency')}
          <input
            type="text"
            required
            value={draft.currency}
            onChange={(event) => setDraft((previous) => ({ ...previous, currency: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.capacity')}
          <input
            type="number"
            min={1}
            required
            value={draft.capacity}
            onChange={(event) => setDraft((previous) => ({ ...previous, capacity: event.target.value }))}
          />
        </label>
        <label>
          {t('adminOfferings.fields.status')}
          <select
            value={draft.status}
            onChange={(event) => setDraft((previous) => ({ ...previous, status: event.target.value }))}
          >
            {OFFERING_STATUSES.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
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

export default AdminOfferingsPage
