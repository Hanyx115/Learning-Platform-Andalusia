import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  getSessionAttendance,
  markSessionAttendance,
  type OfferingType,
  type SessionAttendanceRecord,
} from '../../api/instructor'
import Container from '../../components/Container'

const ATTENDANCE_STATUSES = ['Present', 'Absent', 'Late', 'Excused']

function InstructorSessionAttendancePage() {
  const { t } = useTranslation()
  const { sessionId } = useParams()
  const [searchParams] = useSearchParams()
  const offeringType = (searchParams.get('type') as OfferingType | null) ?? 'Course'

  const [roster, setRoster] = useState<SessionAttendanceRecord[] | null>(null)
  const [drafts, setDrafts] = useState<Record<string, { status: string; note: string }>>({})
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState('')
  const [saveSucceeded, setSaveSucceeded] = useState(false)

  const applyRoster = useCallback((loaded: SessionAttendanceRecord[]) => {
    setRoster(loaded)
    setDrafts(
      Object.fromEntries(
        loaded.map((record) => [record.userId, { status: record.status ?? '', note: record.note ?? '' }]),
      ),
    )
  }, [])

  useEffect(() => {
    let isCurrent = true

    if (!sessionId) {
      return () => {
        isCurrent = false
      }
    }

    getSessionAttendance(sessionId, offeringType)
      .then((loaded) => {
        if (isCurrent) {
          applyRoster(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorAttendance.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [sessionId, offeringType, applyRoster, t])

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  async function handleSave() {
    if (!sessionId) {
      return
    }

    const entries = Object.entries(drafts)
      .map(([userId, draft]) => ({
        userId,
        status: draft.status || null,
        note: draft.status ? draft.note.trim() || null : null,
      }))

    setIsSaving(true)
    setSaveError('')
    setSaveSucceeded(false)

    try {
      const updated = await markSessionAttendance(sessionId, offeringType, entries)
      applyRoster(updated)
      setSaveSucceeded(true)
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('instructorAttendance.errors.save')))
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('instructorAttendance.title')}</h1>
        <p>{t('instructorAttendance.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/instructor/sessions">
          {t('instructorAttendance.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}
      {saveSucceeded ? <p className="auth-message auth-message--success" role="status">{t('instructorAttendance.saved')}</p> : null}

      {roster === null ? (
        <p role="status">{t('instructorAttendance.loading')}</p>
      ) : roster.length === 0 ? (
        <p>{t('instructorAttendance.empty')}</p>
      ) : (
        <>
          <div className="dashboard-list">
            {roster.map((record) => (
              <article key={record.userId} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h2 dir="auto">{record.learnerName}</h2>
                  <span>{record.email}</span>
                </div>
                <label>
                  {t('instructorAttendance.statusLabel')}
                  <select
                    value={drafts[record.userId]?.status ?? ''}
                    onChange={(event) => {
                      const status = event.target.value
                      setDrafts((previous) => ({
                        ...previous,
                        [record.userId]: {
                          ...previous[record.userId],
                          status,
                          note: status ? previous[record.userId]?.note ?? '' : '',
                        },
                      }))
                    }}
                  >
                    <option value="">{t('instructorAttendance.statusUnset')}</option>
                    {ATTENDANCE_STATUSES.map((status) => (
                      <option key={status} value={status}>
                        {t(`instructorAttendance.status.${status}`, status)}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  {t('instructorAttendance.noteLabel')}
                  <input
                    type="text"
                    value={drafts[record.userId]?.note ?? ''}
                    onChange={(event) =>
                      setDrafts((previous) => ({
                        ...previous,
                        [record.userId]: { ...previous[record.userId], note: event.target.value },
                      }))
                    }
                  />
                </label>
              </article>
            ))}
          </div>

          <div className="dashboard-card__actions">
            <button className="button button--primary" type="button" disabled={isSaving} onClick={() => void handleSave()}>
              {isSaving ? t('instructorAttendance.saving') : t('instructorAttendance.save')}
            </button>
          </div>
        </>
      )}
    </section>
  )
}

export default InstructorSessionAttendancePage
