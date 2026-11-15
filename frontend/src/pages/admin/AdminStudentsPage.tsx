import type { FormEvent } from 'react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { getStudentDirectory } from '../../api/admin'
import type { AdminEnrollment } from '../../api/admin'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import type { PaginatedResult } from '../../api/catalog'
import Container from '../../components/Container'
import { CatalogPagination } from '../catalog/CatalogUi'

const DEFAULT_PAGE_SIZE = 10
const pageSizes = [10, 25, 50]

function parsePositiveInteger(value: string | null, fallback: number) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}

function statusBadgeClass(status: string) {
  switch (status) {
    case 'Active':
      return 'status-badge status-badge--positive'
    case 'Cancelled':
      return 'status-badge status-badge--negative'
    default:
      return 'status-badge status-badge--neutral'
  }
}

function AdminStudentsPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const [result, setResult] = useState<PaginatedResult<AdminEnrollment> | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const page = parsePositiveInteger(searchParams.get('page'), 1)
  const requestedPageSize = parsePositiveInteger(searchParams.get('pageSize'), DEFAULT_PAGE_SIZE)
  const pageSize = pageSizes.includes(requestedPageSize) ? requestedPageSize : DEFAULT_PAGE_SIZE
  const search = searchParams.get('search') ?? ''
  const status = searchParams.get('status') ?? ''
  const type = searchParams.get('type') ?? ''

  useEffect(() => {
    let isCurrent = true

    getStudentDirectory({ page, pageSize, search, status, type })
      .then((loaded) => {
        if (isCurrent) setResult(loaded)
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) return

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminStudents.errors.load')))
        }
      })

    return () => { isCurrent = false }
  }, [page, pageSize, search, status, t, type])

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const next = new URLSearchParams()

    for (const key of ['search', 'status', 'type', 'pageSize']) {
      const value = String(data.get(key) ?? '').trim()
      if (value && !(key === 'pageSize' && value === String(DEFAULT_PAGE_SIZE))) next.set(key, value)
    }

    setSearchParams(next)
  }

  function changePage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    if (nextPage === 1) next.delete('page')
    else next.set('page', String(nextPage))
    setSearchParams(next)
  }

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
        <h1>{t('adminStudents.title')}</h1>
        <p>{t('adminStudents.description')}</p>
      </div>

      <form key={searchParams.toString()} className="dashboard-filters" onSubmit={applyFilters}>
        <label>
          {t('adminStudents.filters.search')}
          <input name="search" type="search" defaultValue={search} placeholder={t('adminStudents.filters.searchPlaceholder')} />
        </label>
        <label>
          {t('adminStudents.filters.status')}
          <select name="status" defaultValue={status}>
            <option value="">{t('adminStudents.filters.allStatuses')}</option>
            <option value="Active">{t('adminStudents.statuses.active')}</option>
            <option value="Completed">{t('adminStudents.statuses.completed')}</option>
            <option value="Cancelled">{t('adminStudents.statuses.cancelled')}</option>
          </select>
        </label>
        <label>
          {t('adminStudents.filters.type')}
          <select name="type" defaultValue={type}>
            <option value="">{t('adminStudents.filters.allTypes')}</option>
            <option value="Course">{t('adminStudents.types.course')}</option>
            <option value="Program">{t('adminStudents.types.program')}</option>
          </select>
        </label>
        <label>
          {t('common.pageSize')}
          <select name="pageSize" defaultValue={pageSize}>
            {pageSizes.map((size) => <option key={size} value={size}>{size}</option>)}
          </select>
        </label>
        <div className="dashboard-filters__actions">
          <button className="button button--primary" type="submit">{t('common.applyFilters')}</button>
          <button className="button button--secondary" type="button" onClick={() => setSearchParams({})}>
            {t('common.resetFilters')}
          </button>
        </div>
      </form>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {result === null && !loadError ? (
        <p role="status">{t('adminStudents.loading')}</p>
      ) : result?.items.length === 0 ? (
        <p>{t('adminStudents.empty')}</p>
      ) : result ? (
        <>
          <p className="dashboard-results">{t('adminStudents.results', { count: result.totalCount })}</p>
          <div className="dashboard-list">
            {result.items.map((enrollment) => (
              <article key={enrollment.id} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h2 dir="auto">{enrollment.learnerName}</h2>
                  <span className={statusBadgeClass(enrollment.status)}>{enrollment.status}</span>
                </div>
                <div className="dashboard-card__meta">
                  <span>{enrollment.email}</span>
                  <span dir="auto">{enrollment.courseTitle ?? enrollment.programTitle}</span>
                  <span>{t('enrollments.enrolledOn', { date: new Date(enrollment.enrolledAt).toLocaleDateString() })}</span>
                </div>
              </article>
            ))}
          </div>
          <CatalogPagination page={result.page} totalPages={result.totalPages} onPageChange={changePage} />
        </>
      ) : null}
    </section>
  )
}

export default AdminStudentsPage
