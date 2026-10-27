import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  downloadLearnerAssignmentFile,
  getAssignmentSubmissions,
  gradeSubmission,
  type SubmissionRecord,
} from '../../api/instructor'
import Container from '../../components/Container'
import { saveBlob } from '../../utils/download'

function InstructorAssignmentSubmissionsPage() {
  const { t } = useTranslation()
  const { assignmentId } = useParams()

  const [submissions, setSubmissions] = useState<SubmissionRecord[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [gradeDrafts, setGradeDrafts] = useState<Record<string, { score: string; feedback: string }>>({})
  const [gradingUserId, setGradingUserId] = useState<string | null>(null)
  const [downloadingUserId, setDownloadingUserId] = useState<string | null>(null)
  const [gradeErrors, setGradeErrors] = useState<Record<string, string>>({})

  const applySubmissions = useCallback((loaded: SubmissionRecord[]) => {
    setSubmissions(loaded)
    setGradeDrafts(
      Object.fromEntries(
        loaded.map((submission) => [
          submission.userId,
          { score: submission.score?.toString() ?? '', feedback: submission.feedback ?? '' },
        ]),
      ),
    )
  }, [])

  useEffect(() => {
    let isCurrent = true

    if (!assignmentId) {
      return () => {
        isCurrent = false
      }
    }

    getAssignmentSubmissions(assignmentId)
      .then((loaded) => {
        if (isCurrent) {
          applySubmissions(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorSubmissions.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [assignmentId, applySubmissions, t])

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  async function handleGrade(userId: string) {
    if (!assignmentId) {
      return
    }

    const draft = gradeDrafts[userId]
    const score = Number(draft?.score)

    if (!draft?.score || Number.isNaN(score)) {
      setGradeErrors((previous) => ({ ...previous, [userId]: t('instructorSubmissions.errors.scoreRequired') }))
      return
    }

    setGradingUserId(userId)
    setGradeErrors((previous) => ({ ...previous, [userId]: '' }))

    try {
      const updated = await gradeSubmission(assignmentId, userId, score, draft.feedback.trim() || null)
      setSubmissions((previous) => previous?.map((s) => (s.userId === userId ? updated : s)) ?? null)
    } catch (requestError) {
      setGradeErrors((previous) => ({
        ...previous,
        [userId]: getApiErrorMessage(requestError, t('instructorSubmissions.errors.grade')),
      }))
    } finally {
      setGradingUserId(null)
    }
  }

  async function handleDownload(userId: string, fileName: string) {
    if (!assignmentId) {
      return
    }

    setDownloadingUserId(userId)
    setGradeErrors((previous) => ({ ...previous, [userId]: '' }))

    try {
      saveBlob(await downloadLearnerAssignmentFile(assignmentId, userId), fileName)
    } catch (requestError) {
      setGradeErrors((previous) => ({
        ...previous,
        [userId]: getApiErrorMessage(requestError, t('instructorSubmissions.errors.download')),
      }))
    } finally {
      setDownloadingUserId(null)
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('instructorSubmissions.title')}</h1>
        <p>{t('instructorSubmissions.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/instructor/offerings">
          {t('instructorSubmissions.back')}
        </Link>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {submissions === null ? (
        <p role="status">{t('instructorSubmissions.loading')}</p>
      ) : submissions.length === 0 ? (
        <p>{t('instructorSubmissions.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {submissions.map((submission) => {
            const isBusy = gradingUserId === submission.userId
            const isDownloading = downloadingUserId === submission.userId
            const gradeError = gradeErrors[submission.userId]
            const hasSubmission = submission.submittedAtUtc !== null

            return (
              <article key={submission.userId} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h2 dir="auto">{submission.learnerName}</h2>
                  <span>{submission.email}</span>
                </div>

                {submission.submissionType === 'File' && submission.originalFileName ? (
                  <button
                    className="submission-file-link"
                    type="button"
                    disabled={isDownloading}
                    onClick={() => void handleDownload(submission.userId, submission.originalFileName!)}
                  >
                    {isDownloading ? t('instructorSubmissions.downloading') : submission.originalFileName}
                  </button>
                ) : submission.submissionType === 'Link' && submission.linkUrl ? (
                  <a href={submission.linkUrl} target="_blank" rel="noreferrer">
                    {submission.linkUrl}
                  </a>
                ) : submission.content ? (
                  <p dir="auto">{submission.content}</p>
                ) : (
                  <p className="auth-message auth-message--error">{t('instructorSubmissions.notSubmitted')}</p>
                )}

                {gradeError ? <p className="auth-message auth-message--error" role="alert">{gradeError}</p> : null}

                <label>
                  {t('instructorSubmissions.scoreLabel')}
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    disabled={!hasSubmission}
                    value={gradeDrafts[submission.userId]?.score ?? ''}
                    onChange={(event) =>
                      setGradeDrafts((previous) => ({
                        ...previous,
                        [submission.userId]: { ...previous[submission.userId], score: event.target.value },
                      }))
                    }
                  />
                </label>
                <label>
                  {t('instructorSubmissions.feedbackLabel')}
                  <textarea
                    rows={2}
                    disabled={!hasSubmission}
                    value={gradeDrafts[submission.userId]?.feedback ?? ''}
                    onChange={(event) =>
                      setGradeDrafts((previous) => ({
                        ...previous,
                        [submission.userId]: { ...previous[submission.userId], feedback: event.target.value },
                      }))
                    }
                  />
                </label>

                <div className="dashboard-card__actions">
                  <button
                    className="button button--primary"
                    type="button"
                    disabled={isBusy || !hasSubmission}
                    onClick={() => void handleGrade(submission.userId)}
                  >
                    {isBusy ? t('instructorSubmissions.grading') : t('instructorSubmissions.grade')}
                  </button>
                </div>
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}

export default InstructorAssignmentSubmissionsPage
