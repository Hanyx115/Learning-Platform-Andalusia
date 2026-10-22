import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { createOfferingAssignment, updateAssignment, type Assignment, type AssignmentRequest } from '../../api/content'
import { getOfferingAssignments, type OfferingType } from '../../api/instructor'
import Container from '../../components/Container'

interface AssignmentDraft {
  title: string
  description: string
  dueAtUtc: string
  maxScore: string
}

const emptyDraft: AssignmentDraft = { title: '', description: '', dueAtUtc: '', maxScore: '' }

function toLocalInputValue(isoValue: string) {
  return isoValue ? isoValue.slice(0, 16) : ''
}

function InstructorOfferingAssignmentsPage() {
  const { t } = useTranslation()
  const { offeringId } = useParams()
  const [searchParams] = useSearchParams()
  const offeringType = (searchParams.get('type') as OfferingType | null) ?? 'Course'
  const isAdminContext = useLocation().pathname.startsWith('/admin')
  const backLink = isAdminContext ? '/admin/catalog' : '/instructor/offerings'

  const [assignments, setAssignments] = useState<Assignment[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [draft, setDraft] = useState<AssignmentDraft>(emptyDraft)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [saveError, setSaveError] = useState('')

  const load = useCallback(() => {
    if (!offeringId) {
      return
    }

    getOfferingAssignments(offeringId, offeringType)
      .then((loaded) => setAssignments(loaded))
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorAssignments.errors.load')))
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

  function startEdit(assignment: Assignment) {
    setEditingId(assignment.id)
    setDraft({
      title: assignment.title,
      description: assignment.description ?? '',
      dueAtUtc: toLocalInputValue(assignment.dueAtUtc),
      maxScore: assignment.maxScore.toString(),
    })
  }

  async function handleSave() {
    if (!offeringId) {
      return
    }

    setSaveError('')
    const request: AssignmentRequest = {
      title: draft.title,
      description: draft.description.trim() === '' ? null : draft.description,
      dueAtUtc: new Date(draft.dueAtUtc).toISOString(),
      maxScore: Number(draft.maxScore),
    }

    try {
      if (editingId) {
        await updateAssignment(editingId, request)
      } else {
        await createOfferingAssignment(offeringId, offeringType, request)
      }
      setDraft(emptyDraft)
      setEditingId(null)
      load()
    } catch (requestError) {
      setSaveError(getApiErrorMessage(requestError, t('instructorAssignments.errors.save')))
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('instructorAssignments.title')}</h1>
        <p>{t('instructorAssignments.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to={backLink}>
          {t('instructorAssignments.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}
      {saveError ? <p className="auth-message auth-message--error" role="alert">{saveError}</p> : null}

      {assignments === null ? (
        <p role="status">{t('instructorAssignments.loading')}</p>
      ) : assignments.length === 0 ? (
        <p>{t('instructorAssignments.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {assignments.map((assignment) => (
            <article key={assignment.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{assignment.title}</h2>
                <span>{t('instructorAssignments.maxScore', { score: assignment.maxScore })}</span>
              </div>
              {assignment.description ? <p dir="auto">{assignment.description}</p> : null}
              <div className="dashboard-card__meta">
                <span>
                  {t('instructorAssignments.dueOn', { date: new Date(assignment.dueAtUtc).toLocaleDateString() })}
                </span>
              </div>
              <div className="dashboard-card__actions">
                <button className="button button--secondary" type="button" onClick={() => startEdit(assignment)}>
                  {t('adminCatalog.edit')}
                </button>
                <Link className="button button--primary" to={`/instructor/assignments/${assignment.id}/submissions`}>
                  {t('instructorAssignments.viewSubmissions')}
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
        <h2>{editingId ? t('instructorAssignments.editAssignment') : t('instructorAssignments.newAssignment')}</h2>
        <label>
          {t('instructorAssignments.fields.title')}
          <input
            type="text"
            required
            value={draft.title}
            onChange={(event) => setDraft((previous) => ({ ...previous, title: event.target.value }))}
          />
        </label>
        <label>
          {t('instructorAssignments.fields.description')}
          <textarea
            rows={3}
            value={draft.description}
            onChange={(event) => setDraft((previous) => ({ ...previous, description: event.target.value }))}
          />
        </label>
        <label>
          {t('instructorAssignments.fields.dueAt')}
          <input
            type="datetime-local"
            required
            value={draft.dueAtUtc}
            onChange={(event) => setDraft((previous) => ({ ...previous, dueAtUtc: event.target.value }))}
          />
        </label>
        <label>
          {t('instructorAssignments.fields.maxScore')}
          <input
            type="number"
            min={1}
            step="1"
            required
            value={draft.maxScore}
            onChange={(event) => setDraft((previous) => ({ ...previous, maxScore: event.target.value }))}
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

export default InstructorOfferingAssignmentsPage
