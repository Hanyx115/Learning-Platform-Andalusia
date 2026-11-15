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
  downloadEnrollmentMaterialFile,
  downloadMyAssignmentFile,
  submitAssignmentFile,
  submitAssignmentLink,
  type CourseMaterial,
  type Enrollment,
  type LearnerAssignment,
  type LearnerProgress,
  type UpcomingSession,
} from '../../api/enrollment'
import Container from '../../components/Container'
import { saveBlob } from '../../utils/download'
import CourseAssistantPanel from './CourseAssistantPanel'

type SubmissionMethod = 'link' | 'file'

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

  const [submissionMethods, setSubmissionMethods] = useState<Record<string, SubmissionMethod>>({})
  const [linkDrafts, setLinkDrafts] = useState<Record<string, string>>({})
  const [fileDrafts, setFileDrafts] = useState<Record<string, File | null>>({})
  const [submittingId, setSubmittingId] = useState<string | null>(null)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [downloadingMaterialId, setDownloadingMaterialId] = useState<string | null>(null)
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

  async function handleSubmit(assignment: LearnerAssignment) {
    const assignmentId = assignment.assignmentId
    const method = submissionMethods[assignmentId] ?? (assignment.mySubmissionType === 'File' ? 'file' : 'link')
    const link = (linkDrafts[assignmentId] ?? assignment.mySubmissionLinkUrl ?? '').trim()
    const file = fileDrafts[assignmentId]

    if (method === 'link' && !link) {
      setSubmitErrors((previous) => ({ ...previous, [assignmentId]: t('courseLearning.errors.linkRequired') }))
      return
    }

    if (method === 'file' && !file) {
      setSubmitErrors((previous) => ({ ...previous, [assignmentId]: t('courseLearning.errors.fileRequired') }))
      return
    }

    setSubmittingId(assignmentId)
    setSubmitErrors((previous) => ({ ...previous, [assignmentId]: '' }))

    try {
      if (method === 'link') {
        await submitAssignmentLink(assignmentId, link)
      } else {
        await submitAssignmentFile(assignmentId, file!)
      }
      setFileDrafts((previous) => ({ ...previous, [assignmentId]: null }))
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

  async function handleDownload(assignmentId: string, fileName: string) {
    setDownloadingId(assignmentId)
    setSubmitErrors((previous) => ({ ...previous, [assignmentId]: '' }))

    try {
      saveBlob(await downloadMyAssignmentFile(assignmentId), fileName)
    } catch (requestError) {
      setSubmitErrors((previous) => ({
        ...previous,
        [assignmentId]: getApiErrorMessage(requestError, t('courseLearning.errors.download')),
      }))
    } finally {
      setDownloadingId(null)
    }
  }

  async function handleMaterialDownload(material: CourseMaterial) {
    if (!enrollmentId || !material.originalFileName) {
      return
    }

    setDownloadingMaterialId(material.id)
    setLoadError('')
    try {
      saveBlob(
        await downloadEnrollmentMaterialFile(enrollmentId, material.id),
        material.originalFileName,
      )
    } catch (requestError) {
      setLoadError(getApiErrorMessage(requestError, t('materials.errors.download')))
    } finally {
      setDownloadingMaterialId(null)
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
            <div>
              <h1 dir="auto">{enrollment.courseTitle ?? enrollment.programTitle}</h1>
              <p>{t('courseLearning.description')}</p>
            </div>
            <Link
              className="button button--secondary"
              to={`/community/${enrollment.courseOfferingId ? 'course' : 'program'}/${enrollment.courseOfferingId ?? enrollment.programOfferingId}`}
            >
              {t('community.open')}
            </Link>
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

          <CourseAssistantPanel enrollmentId={enrollment.id} />

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
                      {material.resourceType === 'File' ? (
                        <button
                          className="button button--primary"
                          type="button"
                          disabled={downloadingMaterialId === material.id}
                          onClick={() => void handleMaterialDownload(material)}
                        >
                          {downloadingMaterialId === material.id
                            ? t('materials.downloading')
                            : t('materials.downloadFile')}
                        </button>
                      ) : material.resourceUrl ? (
                        <a className="button button--primary" href={material.resourceUrl} target="_blank" rel="noreferrer">
                          {t('materials.openResource')}
                        </a>
                      ) : null}
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
                  const isDownloading = downloadingId === assignment.assignmentId
                  const hasSubmission = assignment.mySubmittedAtUtc !== null
                  const method = submissionMethods[assignment.assignmentId]
                    ?? (assignment.mySubmissionType === 'File' ? 'file' : 'link')

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

                      {hasSubmission ? (
                        <div className="submission-current-work">
                          <strong>{t('courseLearning.currentWork')}</strong>
                          {assignment.mySubmissionType === 'File' && assignment.mySubmissionFileName ? (
                            <button
                              className="submission-file-link"
                              type="button"
                              disabled={isDownloading}
                              onClick={() => void handleDownload(
                                assignment.assignmentId,
                                assignment.mySubmissionFileName!,
                              )}
                            >
                              {isDownloading
                                ? t('courseLearning.downloading')
                                : assignment.mySubmissionFileName}
                            </button>
                          ) : assignment.mySubmissionType === 'Link' && assignment.mySubmissionLinkUrl ? (
                            <a href={assignment.mySubmissionLinkUrl} target="_blank" rel="noreferrer">
                              {assignment.mySubmissionLinkUrl}
                            </a>
                          ) : assignment.mySubmissionContent ? (
                            <p dir="auto">{assignment.mySubmissionContent}</p>
                          ) : null}
                          <span>
                            {t('courseLearning.submittedOn', {
                              date: new Date(assignment.mySubmittedAtUtc!).toLocaleString(i18n.language),
                            })}
                          </span>
                        </div>
                      ) : null}

                      {submitError ? <p className="auth-message auth-message--error" role="alert">{submitError}</p> : null}

                      {isGraded ? (
                        <div className="dashboard-card__meta">
                          <span className="status-badge status-badge--positive">
                            {t('courseLearning.graded', { score: assignment.myScore, max: assignment.maxScore })}
                          </span>
                          {assignment.myFeedback ? <span dir="auto">{assignment.myFeedback}</span> : null}
                        </div>
                      ) : (
                        <>
                          <div className="submission-composer">
                            <strong>{hasSubmission
                              ? t('courseLearning.replaceSubmission')
                              : t('courseLearning.addWork')}</strong>
                            <div className="submission-methods" role="group" aria-label={t('courseLearning.chooseMethod')}>
                              <button
                                className={`submission-method${method === 'link' ? ' submission-method--active' : ''}`}
                                type="button"
                                aria-pressed={method === 'link'}
                                onClick={() => setSubmissionMethods((previous) => ({
                                  ...previous,
                                  [assignment.assignmentId]: 'link',
                                }))}
                              >
                                {t('courseLearning.linkMethod')}
                              </button>
                              <button
                                className={`submission-method${method === 'file' ? ' submission-method--active' : ''}`}
                                type="button"
                                aria-pressed={method === 'file'}
                                onClick={() => setSubmissionMethods((previous) => ({
                                  ...previous,
                                  [assignment.assignmentId]: 'file',
                                }))}
                              >
                                {t('courseLearning.fileMethod')}
                              </button>
                            </div>

                            {method === 'link' ? (
                              <label>
                                {t('courseLearning.linkLabel')}
                                <input
                                  type="url"
                                  inputMode="url"
                                  placeholder="https://"
                                  value={linkDrafts[assignment.assignmentId] ?? assignment.mySubmissionLinkUrl ?? ''}
                                  onChange={(event) => setLinkDrafts((previous) => ({
                                    ...previous,
                                    [assignment.assignmentId]: event.target.value,
                                  }))}
                                />
                              </label>
                            ) : (
                              <label className="submission-file-picker">
                                <span>{t('courseLearning.fileLabel')}</span>
                                <input
                                  type="file"
                                  accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.txt"
                                  onChange={(event) => setFileDrafts((previous) => ({
                                    ...previous,
                                    [assignment.assignmentId]: event.target.files?.[0] ?? null,
                                  }))}
                                />
                                <small>{t('courseLearning.fileHelp')}</small>
                              </label>
                            )}
                          </div>
                          <div className="dashboard-card__actions">
                            <button
                              className="button button--primary"
                              type="button"
                              disabled={isBusy}
                              onClick={() => void handleSubmit(assignment)}
                            >
                              {isBusy
                                ? t('courseLearning.submitting')
                                : hasSubmission
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
