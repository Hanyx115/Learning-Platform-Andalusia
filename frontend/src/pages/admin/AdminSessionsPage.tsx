import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  cancelCourseSession,
  cancelProgramSession,
  createCourseSession,
  createProgramSession,
  getTrainingLocationsForManagement,
  getUsersInRole,
  rescheduleCourseSession,
  rescheduleProgramSession,
  type ScheduleSessionRequest,
} from '../../api/admin'
import { getCourseOfferingSessions, getProgramOfferingSessions } from '../../api/catalog'
import type { CourseSession, ProgramSession, TrainingLocation } from '../../api/catalog'
import type { Profile } from '../../api/auth'
import Container from '../../components/Container'

type Session = CourseSession | ProgramSession

interface SessionDraft {
  locationId: string
  instructorUserId: string
  title: string
  startsAtUtc: string
  endsAtUtc: string
}

const emptyDraft: SessionDraft = { locationId: '', instructorUserId: '', title: '', startsAtUtc: '', endsAtUtc: '' }

function toLocalInputValue(isoValue: string) {
  return isoValue ? isoValue.slice(0, 16) : ''
}

function AdminSessionsPage() {
  const { t } = useTranslation()
  const { courseOfferingId, programOfferingId } = useParams()
  const offeringId = courseOfferingId ?? programOfferingId ?? ''
  const offeringType = courseOfferingId ? 'Course' : 'Program'

  const [sessions, setSessions] = useState<Session[] | null>(null)
  const [locations, setLocations] = useState<TrainingLocation[] | null>(null)
  const [instructors, setInstructors] = useState<Profile[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [draft, setDraft] = useState<SessionDraft>(emptyDraft)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saveError, setSaveError] = useState('')
  const [cancelReasons, setCancelReasons] = useState<Record<string, string>>({})
  const [actionError, setActionError] = useState('')

  const load = useCallback(() => {
    if (!offeringId) {
      return
    }

    const sessionsRequest = offeringType === 'Course'
      ? getCourseOfferingSessions(offeringId)
      : getProgramOfferingSessions(offeringId)

    Promise.all([sessionsRequest, getTrainingLocationsForManagement(), getUsersInRole('Instructor')])
      .then(([loadedSessions, loadedLocations, loadedInstructors]) => {
        setSessions(loadedSessions)
        setLocations(loadedLocations)
        setInstructors(loadedInstructors)
      })
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminSessions.errors.load')))
        }
      })
  }, [offeringId, offeringType, t])

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

  function startEdit(session: Session) {
    setEditingId(session.id)
    setDraft({
      locationId: session.location?.id ?? '',
      instructorUserId: session.instructorUserId ?? '',
      title: session.title,
      startsAtUtc: toLocalInputValue(session.startsAtUtc),
      endsAtUtc: toLocalInputValue(session.endsAtUtc),
    })
  }

  async function handleSave() {
    setSaveError('')
    const request: ScheduleSessionRequest = {
      locationId: draft.locationId || null,
      instructorUserId: draft.instructorUserId,
      title: draft.title.trim(),
      startsAtUtc: new Date(draft.startsAtUtc).toISOString(),
      endsAtUtc: new Date(draft.endsAtUtc).toISOString(),
    }

    try {
      if (editingId) {
        if (offeringType === 'Course') {
          await rescheduleCourseSession(editingId, request)
        } else {
          await rescheduleProgramSession(editingId, request)
        }
      } else if (offeringType === 'Course') {
        await createCourseSession(offeringId, request)
      } else {
        await createProgramSession(offeringId, request)
      }
      setDraft(emptyDraft)
      setEditingId(null)
      load()
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('adminSessions.errors.save')))
    }
  }

  async function handleCancel(sessionId: string) {
    const reason = cancelReasons[sessionId]?.trim()
    if (!reason) {
      setActionError(t('adminSessions.errors.reasonRequired'))
      return
    }

    setActionError('')
    try {
      if (offeringType === 'Course') {
        await cancelCourseSession(sessionId, reason)
      } else {
        await cancelProgramSession(sessionId, reason)
      }
      load()
    } catch (requestError) {
      setActionError(getApiErrorMessage(requestError, t('adminSessions.errors.cancel')))
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('adminSessions.title')}</h1>
        <p>{t('adminSessions.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/admin/catalog">
          {t('adminOfferings.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}
      {actionError ? <p className="auth-message auth-message--error" role="alert">{actionError}</p> : null}

      {sessions === null ? (
        <p role="status">{t('adminSessions.loading')}</p>
      ) : sessions.length === 0 ? (
        <p>{t('adminSessions.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {sessions.map((session) => (
            <article key={session.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{session.title}</h2>
                <span className="status-badge status-badge--neutral">{session.status}</span>
              </div>
              <div className="dashboard-card__meta">
                <span>{new Date(session.startsAtUtc).toLocaleString()}</span>
                <span>{session.instructorName ?? t('adminSessions.noInstructor')}</span>
                {session.location ? <span>{session.location.branchName}</span> : <span>{t('schedule.online')}</span>}
                {session.cancellationReason ? <span>{t('schedule.cancellationReason', { reason: session.cancellationReason })}</span> : null}
              </div>
              {session.status !== 'Cancelled' ? (
                <div className="dashboard-card__actions">
                  <button className="button button--secondary" type="button" onClick={() => startEdit(session)}>
                    {t('adminSessions.reschedule')}
                  </button>
                  <input
                    type="text"
                    placeholder={t('adminSessions.cancelReasonPlaceholder')}
                    value={cancelReasons[session.id] ?? ''}
                    onChange={(event) =>
                      setCancelReasons((previous) => ({ ...previous, [session.id]: event.target.value }))
                    }
                  />
                  <button className="button button--secondary" type="button" onClick={() => void handleCancel(session.id)}>
                    {t('adminSessions.cancel')}
                  </button>
                </div>
              ) : null}
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
        <h2>{editingId ? t('adminSessions.editSession') : t('adminSessions.newSession')}</h2>
        <label>
          {t('adminSessions.fields.title')}
          <input
            type="text"
            required
            value={draft.title}
            onChange={(event) => setDraft((previous) => ({ ...previous, title: event.target.value }))}
          />
        </label>
        <label>
          {t('adminSessions.fields.instructor')}
          <select
            required
            value={draft.instructorUserId}
            onChange={(event) => setDraft((previous) => ({ ...previous, instructorUserId: event.target.value }))}
          >
            <option value="">{t('adminSessions.fields.selectInstructor')}</option>
            {instructors?.map((instructor) => (
              <option key={instructor.id} value={instructor.id}>
                {instructor.firstName} {instructor.lastName}
              </option>
            ))}
          </select>
        </label>
        <label>
          {t('adminSessions.fields.location')}
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

export default AdminSessionsPage
