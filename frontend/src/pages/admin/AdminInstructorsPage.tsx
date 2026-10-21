import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getUsersInRole } from '../../api/admin'
import type { Profile } from '../../api/auth'
import Container from '../../components/Container'

function AdminInstructorsPage() {
  const { t } = useTranslation()
  const [instructors, setInstructors] = useState<Profile[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  useEffect(() => {
    let isCurrent = true

    getUsersInRole('Instructor')
      .then((loaded) => {
        if (isCurrent) {
          setInstructors(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminInstructors.errors.load')))
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
        <h1>{t('adminInstructors.title')}</h1>
        <p>{t('adminInstructors.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {instructors === null ? (
        <p role="status">{t('adminInstructors.loading')}</p>
      ) : instructors.length === 0 ? (
        <p>{t('adminInstructors.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {instructors.map((instructor) => (
            <article key={instructor.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{instructor.firstName} {instructor.lastName}</h2>
              </div>
              <div className="dashboard-card__meta">
                <span>{instructor.email}</span>
                <span>{instructor.phoneNumber}</span>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default AdminInstructorsPage
