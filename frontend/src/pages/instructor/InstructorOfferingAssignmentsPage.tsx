import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getOfferingAssignments, type Assignment, type OfferingType } from '../../api/instructor'
import Container from '../../components/Container'

function InstructorOfferingAssignmentsPage() {
  const { t } = useTranslation()
  const { offeringId } = useParams()
  const [searchParams] = useSearchParams()
  const offeringType = (searchParams.get('type') as OfferingType | null) ?? 'Course'

  const [assignments, setAssignments] = useState<Assignment[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    if (!offeringId) {
      return () => {
        isCurrent = false
      }
    }

    getOfferingAssignments(offeringId, offeringType)
      .then((loaded) => {
        if (isCurrent) {
          setAssignments(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorAssignments.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [offeringId, offeringType, t])

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('instructorAssignments.title')}</h1>
        <p>{t('instructorAssignments.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/instructor/offerings">
          {t('instructorAssignments.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

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
                <Link className="button button--primary" to={`/instructor/assignments/${assignment.id}/submissions`}>
                  {t('instructorAssignments.viewSubmissions')}
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default InstructorOfferingAssignmentsPage
