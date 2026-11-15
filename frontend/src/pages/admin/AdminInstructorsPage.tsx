import type { FormEvent } from 'react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { getInstructorDirectory } from '../../api/admin'
import type { DirectoryUser } from '../../api/admin'
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

function AdminInstructorsPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const [result, setResult] = useState<PaginatedResult<DirectoryUser> | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const page = parsePositiveInteger(searchParams.get('page'), 1)
  const requestedPageSize = parsePositiveInteger(searchParams.get('pageSize'), DEFAULT_PAGE_SIZE)
  const pageSize = pageSizes.includes(requestedPageSize) ? requestedPageSize : DEFAULT_PAGE_SIZE
  const search = searchParams.get('search') ?? ''
  const status = searchParams.get('status') ?? ''

  useEffect(() => {
    let isCurrent = true

    getInstructorDirectory({ page, pageSize, search, status })
      .then((loaded) => {
        if (isCurrent) setResult(loaded)
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) return

        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminInstructors.errors.load')))
        }
      })

    return () => { isCurrent = false }
  }, [page, pageSize, search, status, t])

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const next = new URLSearchParams()

    for (const key of ['search', 'status', 'pageSize']) {
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
        <h1>{t('adminInstructors.title')}</h1>
        <p>{t('adminInstructors.description')}</p>
      </div>

      <form key={searchParams.toString()} className="dashboard-filters" onSubmit={applyFilters}>
        <label>
          {t('adminInstructors.filters.search')}
          <input name="search" type="search" defaultValue={search} placeholder={t('adminInstructors.filters.searchPlaceholder')} />
        </label>
        <label>
          {t('adminInstructors.filters.status')}
          <select name="status" defaultValue={status}>
            <option value="">{t('adminInstructors.filters.allStatuses')}</option>
            <option value="Active">{t('adminInstructors.statuses.active')}</option>
            <option value="Inactive">{t('adminInstructors.statuses.inactive')}</option>
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
        <p role="status">{t('adminInstructors.loading')}</p>
      ) : result?.items.length === 0 ? (
        <p>{t('adminInstructors.empty')}</p>
      ) : result ? (
        <>
          <p className="dashboard-results">{t('adminInstructors.results', { count: result.totalCount })}</p>
          <div className="dashboard-list">
            {result.items.map((instructor) => (
              <article key={instructor.id} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h2 dir="auto">{instructor.firstName} {instructor.lastName}</h2>
                  <span className={`status-badge ${instructor.isActive ? 'status-badge--positive' : 'status-badge--negative'}`}>
                    {instructor.isActive
                      ? t('adminInstructors.statuses.active')
                      : t('adminInstructors.statuses.inactive')}
                  </span>
                </div>
                <div className="dashboard-card__meta">
                  <span>{instructor.email}</span>
                  <span>{instructor.phoneNumber}</span>
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

export default AdminInstructorsPage
