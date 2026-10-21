import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getMyInstructorStudents, type InstructorStudent } from '../../api/instructor'
import Container from '../../components/Container'

function InstructorStudentsPage() {
  const { t } = useTranslation()
  const [students, setStudents] = useState<InstructorStudent[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getMyInstructorStudents()
      .then((loaded) => {
        if (isCurrent) {
          setStudents(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('instructorStudents.errors.load')))
        }
      })

    return () => {
      isCurrent = false
    }
  }, [t])

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
        <h1>{t('instructorStudents.title')}</h1>
        <p>{t('instructorStudents.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {students === null ? (
        <p role="status">{t('instructorStudents.loading')}</p>
      ) : students.length === 0 ? (
        <p>{t('instructorStudents.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {students.map((student) => (
            <article key={`${student.userId}-${student.courseOfferingId ?? student.programOfferingId}`} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{student.learnerName}</h2>
                <span>{student.email}</span>
              </div>
              <div className="dashboard-card__meta">
                <span dir="auto">{student.offeringTitle}</span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default InstructorStudentsPage
