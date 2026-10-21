import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  getEnrollmentMaterials,
  getMyAssignments,
  getMyEnrollments,
  getMyProgress,
  getMyUpcomingSessions,
  submitAssignment,
  type CourseMaterial,
  type Enrollment,
  type LearnerAssignment,
  type LearnerProgress,
  type UpcomingSession,
} from '../../api/enrollment'
import Container from '../../components/Container'

function CourseLearningPage() {
  const { t, i18n } = useTranslation()
  const { enrollmentId } = useParams()

  const [enrollment, setEnrollment] = useState<Enrollment | null>(null)
  const [sessions, setSessions] = useState<UpcomingSession[]>([])
  const [materials, setMaterials] = useState<CourseMaterial[]>([])
  const [assignments, setAssignments] = useState<LearnerAssignment[]>([])
  const [progress, setProgress] = useState<LearnerProgress | null>(null)
  const [loadError, setLoadError] = useState('')
  const [notFound, setNotFound] = useState(false)
  const [isForbidden, setIsForbidden] = useState(false)
  const [isLoaded, setIsLoaded] = useState(false)

  const [drafts, setDrafts] = useState<Record<string, string>>({})
  const [submittingId, setSubmittingId] = useState<string | null>(null)
  const [submitErrors, setSubmitErrors] = useState<Record<string, string>>({})

  const load = useCallback(() => {
    if (!enrollmentId) {
      return
    }

    Promise.all([
      getMyEnrollments(),
      getEnrollmentMaterials(enrollmentId),
      getMyUpcomingSessions(),
      getMyAssignments(),
      getMyProgress(),
    ])
      .then(([enrollments, loadedMaterials, loadedSessions, loadedAssignments, loadedProgress]) => {
        const matched = enrollments.find((item) => item.id === enrollmentId)
        if (!matched) {
          setNotFound(true)
          return
        }

        setEnrollment(matched)
        setMaterials(loadedMaterials)
        setSessions(loadedSessions.filter((session) => session.enrollmentId === enrollmentId))
        setAssignments(
          loadedAssignments.filter(
            (assignment) =>
              (matched.courseOfferingId !== null && assignment.courseOfferingId === matched.courseOfferingId) ||
              (matched.programOfferingId !== null && assignment.programOfferingId === matched.programOfferingId),
          ),
        )
        setProgress(loadedProgress.find((item) => item.enrollmentId === enrollmentId) ?? null)
        setIsLoaded(true)
      })
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('courseLearning.errors.load')))
        }
      })
  }, [enrollmentId, t])

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

  async function handleSubmit(assignmentId: string) {
    const content = drafts[assignmentId]?.trim()
    if (!content) {
      setSubmitErrors((previous) => ({ ...previous, [assignmentId]: t('courseLearning.errors.contentRequired') }))
      return
    }

    setSubmittingId(assignmentId)
    setSubmitErrors((previous) => ({ ...previous, [assignmentId]: '' }))

    try {
      await submitAssignment(assignmentId, content)
      load()
    } catch (requestError) {
      setSubmitErrors((previous) => ({
        ...previous,
        [assignmentId]: getApiErrorMessage(requestError, t('courseLearning.errors.submit')),
      }))
    } finally {
      setSubmittingId(null)
    }
  }

  return (
    <section className="dashboard-section">
      <div>
        <Link className="button button--secondary" to="/profile/enrollments">
          {t('materials.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {notFound ? (
        <p className="auth-message auth-message--error" role="alert">{t('materials.errors.notFound')}</p>
      ) : !isLoaded || !enrollment ? (
        <p role="status">{t('courseLearning.loading')}</p>
      ) : (
        <>
          <div className="dashboard-section__heading">
            <h1 dir="auto">{enrollment.courseTitle ?? enrollment.programTitle}</h1>
            <p>{t('courseLearning.description')}</p>
          </div>

          {progress ? (
            <article className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2>{t('courseLearning.progressTitle')}</h2>
                <span className="status-badge status-badge--positive">
                  {t('instructorProgress.overall', { percentage: progress.overallProgressPercentage })}
                </span>
              </div>
              <div className="dashboard-card__meta">
                <span>
                  {t('instructorProgress.attendance', {
                    attended: progress.attendedSessions,
                    total: progress.totalSessions,
                    percentage: progress.attendanceRatePercentage,
                  })}
                </span>
                <span>
                  {t('instructorProgress.assignments', {
                    submitted: progress.submittedAssignments,
                    total: progress.totalAssignments,
                  })}
                </span>
                {progress.averageScorePercentage !== null ? (
                  <span>{t('instructorProgress.averageScore', { percentage: progress.averageScorePercentage })}</span>
                ) : null}
              </div>
            </article>
          ) : null}

          {/* Schedule */}
          <section className="dashboard-section">
            <h2>{t('courseLearning.scheduleTitle')}</h2>
            {sessions.length === 0 ? (
              <p>{t('schedule.myEmpty')}</p>
            ) : (
              <div className="dashboard-list">
                {sessions.map((session) => (
                  <article key={session.sessionId} className="dashboard-card schedule-card">
                    <div className="dashboard-card__heading">
                      <strong dir="auto">{session.sessionTitle}</strong>
                      <time dateTime={session.startsAtUtc}>
                        {new Date(session.startsAtUtc).toLocaleString(i18n.language)}
                      </time>
                    </div>
                    <div className="dashboard-card__meta">
                      {session.location ? (
                        <span>
                          {t('schedule.location', {
                            branch: session.location.branchName,
                            room: session.location.roomName,
                            city: session.location.city,
                          })}
                        </span>
                      ) : (
                        <span>{t('schedule.online')}</span>
                      )}
                      {session.instructorName ? (
                        <span>{t('schedule.instructor', { name: session.instructorName })}</span>
                      ) : null}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>

          {/* Materials */}
          <section className="dashboard-section">
            <h2>{t('materials.title')}</h2>
            {materials.length === 0 ? (
              <p>{t('materials.empty')}</p>
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
                    <div className="dashboard-card__actions">
                      <a className="button button--primary" href={material.resourceUrl} target="_blank" rel="noreferrer">
                        {t('materials.openResource')}
                      </a>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>

          {/* Assignments */}
          <section className="dashboard-section">
            <h2>{t('courseLearning.assignmentsTitle')}</h2>
            {assignments.length === 0 ? (
              <p>{t('courseLearning.assignmentsEmpty')}</p>
            ) : (
              <div className="dashboard-list">
                {assignments.map((assignment) => {
                  const isGraded = assignment.myScore !== null
                  const submitError = submitErrors[assignment.assignmentId]
                  const isBusy = submittingId === assignment.assignmentId

                  return (
                    <article key={assignment.assignmentId} className="dashboard-card">
                      <div className="dashboard-card__heading">
                        <h3 dir="auto">{assignment.title}</h3>
                        <span>{t('instructorAssignments.maxScore', { score: assignment.maxScore })}</span>
                      </div>
                      {assignment.description ? <p dir="auto">{assignment.description}</p> : null}
                      <div className="dashboard-card__meta">
                        <span>
                          {t('instructorAssignments.dueOn', { date: new Date(assignment.dueAtUtc).toLocaleDateString() })}
                        </span>
                      </div>

                      {isGraded ? (
                        <div className="dashboard-card__meta">
                          <span className="status-badge status-badge--positive">
                            {t('courseLearning.graded', { score: assignment.myScore, max: assignment.maxScore })}
                          </span>
                          {assignment.myFeedback ? <span dir="auto">{assignment.myFeedback}</span> : null}
                        </div>
                      ) : (
                        <>
                          {submitError ? <p className="auth-message auth-message--error" role="alert">{submitError}</p> : null}
                          <label>
                            {t('courseLearning.submissionLabel')}
                            <textarea
                              rows={3}
                              value={drafts[assignment.assignmentId] ?? assignment.mySubmissionContent ?? ''}
                              onChange={(event) =>
                                setDrafts((previous) => ({ ...previous, [assignment.assignmentId]: event.target.value }))
                              }
                            />
                          </label>
                          <div className="dashboard-card__actions">
                            <button
                              className="button button--primary"
                              type="button"
                              disabled={isBusy}
                              onClick={() => void handleSubmit(assignment.assignmentId)}
                            >
                              {isBusy
                                ? t('courseLearning.submitting')
                                : assignment.mySubmissionContent
                                  ? t('courseLearning.updateSubmission')
                                  : t('courseLearning.submit')}
                            </button>
                          </div>
                        </>
                      )}
                    </article>
                  )
                })}
              </div>
            )}
          </section>
        </>
      )}
    </section>
  )
}

export default CourseLearningPage
