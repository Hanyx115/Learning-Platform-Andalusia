import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import { getEnrollmentMaterials, type CourseMaterial } from '../../api/enrollment'

function MaterialsPage() {
  const { t } = useTranslation()
  const { enrollmentId } = useParams()
  const [materials, setMaterials] = useState<CourseMaterial[] | null>(null)
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    let isCurrent = true

    if (!enrollmentId) {
      return () => {
        isCurrent = false
      }
    }

    getEnrollmentMaterials(enrollmentId)
      .then((loaded) => {
        if (isCurrent) {
          setMaterials(loaded)
        }
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) {
          return
        }

        setLoadError(
          getApiErrorStatus(requestError) === 404
            ? t('materials.errors.notFound')
            : getApiErrorMessage(requestError, t('materials.errors.load')),
        )
      })

    return () => {
      isCurrent = false
    }
  }, [enrollmentId, t])

  const displayedError = enrollmentId ? loadError : t('materials.errors.notFound')

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('materials.title')}</h1>
        <p>{t('materials.description')}</p>
      </div>

      <div>
        <Link className="button button--secondary" to="/profile/enrollments">
          {t('materials.back')}
        </Link>
      </div>

      {displayedError ? (
        <p className="auth-message auth-message--error" role="alert">{displayedError}</p>
      ) : null}

      {materials === null && !displayedError ? (
        <p role="status">{t('materials.loading')}</p>
      ) : materials?.length === 0 ? (
        <p>{t('materials.empty')}</p>
      ) : (
        <div className="dashboard-list">
          {materials?.map((material) => (
            <article key={material.id} className="dashboard-card">
              <div className="dashboard-card__heading">
                <h2 dir="auto">{material.title}</h2>
                <span className="status-badge status-badge--neutral">
                  {t(`materials.types.${material.materialType}`, material.materialType)}
                </span>
              </div>
              {material.description ? <p dir="auto">{material.description}</p> : null}
              {material.sessionTitle ? (
                <div className="dashboard-card__meta">
                  <span>{t('materials.session', { title: material.sessionTitle })}</span>
                </div>
              ) : null}
              <div className="dashboard-card__actions">
                <a
                  className="button button--primary"
                  href={material.resourceUrl}
                  target="_blank"
                  rel="noreferrer"
                >
                  {t('materials.openResource')}
                </a>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}

export default MaterialsPage
