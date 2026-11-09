import type { FormEvent } from 'react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  acceptApplication,
  getApplicationsPage,
  rejectApplication,
  type Application,
  type ApplicationSort,
  type PaginatedResult,
} from '../../api/enrollment'
import Container from '../../components/Container'
import { CatalogPagination } from '../catalog/CatalogUi'
import '../profile/profile-dashboard.css'

type RejectionChoice = 'prerequisites' | 'capacity' | 'incomplete' | 'other'

const rejectionChoices: RejectionChoice[] = ['prerequisites', 'capacity', 'incomplete', 'other']
const DEFAULT_PAGE_SIZE = 10
const DEFAULT_SORT: ApplicationSort = 'submitted_desc'
const pageSizes = [10, 25, 50]
const supportedSorts: ApplicationSort[] = [
  DEFAULT_SORT,
  'learner_asc',
  'learner_desc',
  'offering_asc',
  'offering_desc',
  'intake_asc',
  'intake_desc',
]

function parsePositiveInteger(value: string | null, fallback: number) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback
}

function StaffApplicationsPage() {
  const { t, i18n } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const [result, setResult] = useState<PaginatedResult<Application> | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)
  const [actioningId, setActioningId] = useState<string | null>(null)
  const [actionErrors, setActionErrors] = useState<Record<string, string>>({})
  const [expandedRejectionId, setExpandedRejectionId] = useState<string | null>(null)
  const [rejectChoices, setRejectChoices] = useState<Record<string, RejectionChoice | undefined>>({})
  const [rejectDrafts, setRejectDrafts] = useState<Record<string, string>>({})

  const page = parsePositiveInteger(searchParams.get('page'), 1)
  const requestedPageSize = parsePositiveInteger(searchParams.get('pageSize'), DEFAULT_PAGE_SIZE)
  const pageSize = pageSizes.includes(requestedPageSize) ? requestedPageSize : DEFAULT_PAGE_SIZE
  const learnerName = searchParams.get('learnerName') ?? ''
  const offeringTitle = searchParams.get('offeringTitle') ?? ''
  const intakeFrom = searchParams.get('intakeFrom') ?? ''
  const intakeTo = searchParams.get('intakeTo') ?? ''
  const requestedSort = searchParams.get('sort') as ApplicationSort | null
  const sort = requestedSort && supportedSorts.includes(requestedSort) ? requestedSort : DEFAULT_SORT

  useEffect(() => {
    let isCurrent = true

    getApplicationsPage({
      status: 'Pending',
      page,
      pageSize,
      learnerName,
      offeringTitle,
      intakeFrom,
      intakeTo,
      sort,
    })
      .then((loadedResult) => {
        if (!isCurrent) return
        setResult(loadedResult)
        setLoadError('')
        setIsForbidden(false)
      })
      .catch((requestError: unknown) => {
        if (!isCurrent) return
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('staffApplications.errors.load')))
        }
      })

    return () => { isCurrent = false }
  }, [intakeFrom, intakeTo, learnerName, offeringTitle, page, pageSize, refreshKey, sort, t])

  function refreshAfterReview(applicationId: string) {
    setExpandedRejectionId((current) => current === applicationId ? null : current)
    if (result?.items.length === 1 && page > 1) {
      changePage(page - 1)
    } else {
      setRefreshKey((current) => current + 1)
    }
  }

  async function handleAccept(applicationId: string) {
    setActioningId(applicationId)
    setActionErrors((previous) => ({ ...previous, [applicationId]: '' }))

    try {
      await acceptApplication(applicationId)
      refreshAfterReview(applicationId)
    } catch (requestError) {
      setActionErrors((previous) => ({
        ...previous,
        [applicationId]: getApiErrorMessage(requestError, t('staffApplications.errors.accept')),
      }))
    } finally {
      setActioningId(null)
    }
  }

  function getRejectionReason(applicationId: string) {
    const choice = rejectChoices[applicationId]
    if (!choice) return ''
    if (choice === 'other') return rejectDrafts[applicationId]?.trim() ?? ''
    return t(`staffApplications.rejectionReasons.${choice}.message`)
  }

  async function handleReject(applicationId: string) {
    const reason = getRejectionReason(applicationId)
    if (!reason) {
      setActionErrors((previous) => ({
        ...previous,
        [applicationId]: t('staffApplications.errors.reasonRequired'),
      }))
      return
    }

    setActioningId(applicationId)
    setActionErrors((previous) => ({ ...previous, [applicationId]: '' }))

    try {
      await rejectApplication(applicationId, reason)
      refreshAfterReview(applicationId)
    } catch (requestError) {
      setActionErrors((previous) => ({
        ...previous,
        [applicationId]: getApiErrorMessage(requestError, t('staffApplications.errors.reject')),
      }))
    } finally {
      setActioningId(null)
    }
  }

  function openRejection(applicationId: string) {
    setExpandedRejectionId(applicationId)
    setActionErrors((previous) => ({ ...previous, [applicationId]: '' }))
  }

  function closeRejection(applicationId: string) {
    setExpandedRejectionId(null)
    setActionErrors((previous) => ({ ...previous, [applicationId]: '' }))
  }

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const next = new URLSearchParams()

    for (const key of ['learnerName', 'offeringTitle', 'intakeFrom', 'intakeTo']) {
      const value = String(data.get(key) ?? '').trim()
      if (value) next.set(key, value)
    }

    const nextSort = String(data.get('sort') ?? DEFAULT_SORT)
    if (nextSort !== DEFAULT_SORT) next.set('sort', nextSort)

    const nextPageSize = String(data.get('pageSize') ?? DEFAULT_PAGE_SIZE)
    if (nextPageSize !== String(DEFAULT_PAGE_SIZE)) next.set('pageSize', nextPageSize)

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
    <div className="profile-dashboard">
      <Container>
        <section className="dashboard-section">
          <div className="dashboard-section__heading">
            <h1>{t('staffApplications.title')}</h1>
            <p>{t('staffApplications.description')}</p>
          </div>

          <form key={searchParams.toString()} className="dashboard-filters application-review-filters" onSubmit={applyFilters}>
            <label>
              {t('staffApplications.filters.learnerName')}
              <input
                name="learnerName"
                type="search"
                maxLength={100}
                defaultValue={learnerName}
                placeholder={t('staffApplications.filters.learnerPlaceholder')}
              />
            </label>
            <label>
              {t('staffApplications.filters.offeringTitle')}
              <input
                name="offeringTitle"
                type="search"
                maxLength={200}
                defaultValue={offeringTitle}
                placeholder={t('staffApplications.filters.offeringPlaceholder')}
              />
            </label>
            <label>
              {t('staffApplications.filters.intakeFrom')}
              <input name="intakeFrom" type="date" defaultValue={intakeFrom} />
            </label>
            <label>
              {t('staffApplications.filters.intakeTo')}
              <input name="intakeTo" type="date" defaultValue={intakeTo} min={intakeFrom || undefined} />
            </label>
            <label>
              {t('staffApplications.filters.sort')}
              <select name="sort" defaultValue={sort}>
                {supportedSorts.map((sortOption) => (
                  <option key={sortOption} value={sortOption}>
                    {t(`staffApplications.sorts.${sortOption}`)}
                  </option>
                ))}
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
            <p role="status">{t('staffApplications.loading')}</p>
          ) : result?.items.length === 0 ? (
            <div className="dashboard-empty-state">
              <strong>{t(
                learnerName || offeringTitle || intakeFrom || intakeTo
                  ? 'staffApplications.noMatchesTitle'
                  : 'staffApplications.emptyTitle',
              )}</strong>
              <p>{t(
                learnerName || offeringTitle || intakeFrom || intakeTo
                  ? 'staffApplications.noMatches'
                  : 'staffApplications.empty',
              )}</p>
            </div>
          ) : result ? (
            <>
              <p className="dashboard-results">
                {t('staffApplications.results', { count: result.totalCount })}
              </p>
              <div className="dashboard-list application-review-list">
              {result.items.map((application) => {
                const title = application.courseTitle ?? application.programTitle ?? ''
                const applicantName = `${application.applicantFirstName} ${application.applicantLastName}`.trim()
                const initials = `${application.applicantFirstName.charAt(0)}${application.applicantLastName.charAt(0)}`.toUpperCase()
                const actionError = actionErrors[application.id]
                const isBusy = actioningId === application.id
                const isRejecting = expandedRejectionId === application.id
                const selectedReason = rejectChoices[application.id]
                const offeringType = application.courseOfferingId ? 'course' : 'program'
                const dateFormatter = new Intl.DateTimeFormat(i18n.language, { dateStyle: 'medium' })
                const priceFormatter = new Intl.NumberFormat(i18n.language, {
                  style: 'currency',
                  currency: application.currency,
                  maximumFractionDigits: 2,
                })

                return (
                  <article key={application.id} className="dashboard-card application-review-card">
                    <div className="dashboard-card__heading application-review-card__heading">
                      <div>
                        <span className="application-review-card__eyebrow">
                          {t(`staffApplications.offeringTypes.${offeringType}`)}
                        </span>
                        <h2 dir="auto">{title}</h2>
                      </div>
                      <span className="status-badge status-badge--pending">
                        {t(`applications.status.${application.status}`, application.status)}
                      </span>
                    </div>

                    <div className="application-review-applicant">
                      <span className="application-review-applicant__avatar" aria-hidden="true">{initials}</span>
                      <div>
                        <span>{t('staffApplications.applicant')}</span>
                        <strong dir="auto">{applicantName}</strong>
                        <a href={`mailto:${application.applicantEmail}`}>{application.applicantEmail}</a>
                      </div>
                      <Link
                        className="button button--secondary application-review-applicant__profile"
                        to={`/staff/applicants/${application.userId}`}
                      >
                        {t('staffApplications.viewProfile')}
                      </Link>
                    </div>

                    <dl className="application-review-details">
                      <div>
                        <dt>{t('staffApplications.appliedOn')}</dt>
                        <dd>{dateFormatter.format(new Date(application.submittedAt))}</dd>
                      </div>
                      <div>
                        <dt>{t('staffApplications.intakeDates')}</dt>
                        <dd>
                          {dateFormatter.format(new Date(application.startsAtUtc))}
                          {' – '}
                          {dateFormatter.format(new Date(application.endsAtUtc))}
                        </dd>
                      </div>
                      <div>
                        <dt>{t('staffApplications.fee')}</dt>
                        <dd>{priceFormatter.format(application.price)}</dd>
                      </div>
                    </dl>

                    {actionError ? <p className="auth-message auth-message--error" role="alert">{actionError}</p> : null}

                    {!isRejecting ? (
                      <div className="dashboard-card__actions application-review-actions">
                        <button
                          className="button button--primary"
                          type="button"
                          disabled={isBusy}
                          onClick={() => void handleAccept(application.id)}
                        >
                          {t('staffApplications.accept')}
                        </button>
                        <button
                          className="button button--secondary"
                          type="button"
                          disabled={isBusy}
                          onClick={() => openRejection(application.id)}
                        >
                          {t('staffApplications.startRejection')}
                        </button>
                      </div>
                    ) : (
                      <div className="application-rejection-panel">
                        <div className="application-rejection-panel__heading">
                          <h3>{t('staffApplications.rejectionTitle')}</h3>
                          <p>{t('staffApplications.rejectionHelp')}</p>
                        </div>

                        <fieldset className="application-rejection-options">
                          <legend>{t('staffApplications.rejectionReasonLabel')}</legend>
                          {rejectionChoices.map((choice) => (
                            <label key={choice} className={selectedReason === choice ? 'is-selected' : ''}>
                              <input
                                type="radio"
                                name={`rejection-${application.id}`}
                                value={choice}
                                checked={selectedReason === choice}
                                onChange={() => {
                                  setRejectChoices((previous) => ({ ...previous, [application.id]: choice }))
                                  setActionErrors((previous) => ({ ...previous, [application.id]: '' }))
                                }}
                              />
                              <span>
                                <strong>{t(`staffApplications.rejectionReasons.${choice}.label`)}</strong>
                                <small>{t(`staffApplications.rejectionReasons.${choice}.description`)}</small>
                              </span>
                            </label>
                          ))}
                        </fieldset>

                        {selectedReason === 'other' ? (
                          <div className="application-rejection-other">
                            <label htmlFor={`rejection-other-${application.id}`}>
                              {t('staffApplications.otherReasonLabel')}
                            </label>
                            <textarea
                              id={`rejection-other-${application.id}`}
                              aria-describedby={`rejection-other-count-${application.id}`}
                              rows={3}
                              maxLength={500}
                              autoFocus
                              value={rejectDrafts[application.id] ?? ''}
                              placeholder={t('staffApplications.otherReasonPlaceholder')}
                              onChange={(event) => {
                                setRejectDrafts((previous) => ({ ...previous, [application.id]: event.target.value }))
                                setActionErrors((previous) => ({ ...previous, [application.id]: '' }))
                              }}
                            />
                            <small id={`rejection-other-count-${application.id}`}>
                              {t('staffApplications.characterCount', { count: rejectDrafts[application.id]?.length ?? 0 })}
                            </small>
                          </div>
                        ) : selectedReason ? (
                          <p className="application-rejection-preview">
                            <strong>{t('staffApplications.reasonPreview')}</strong>
                            {getRejectionReason(application.id)}
                          </p>
                        ) : null}

                        <div className="dashboard-card__actions">
                          <button
                            className="button button--danger"
                            type="button"
                            disabled={isBusy}
                            onClick={() => void handleReject(application.id)}
                          >
                            {isBusy ? t('staffApplications.rejecting') : t('staffApplications.confirmReject')}
                          </button>
                          <button
                            className="button button--secondary"
                            type="button"
                            disabled={isBusy}
                            onClick={() => closeRejection(application.id)}
                          >
                            {t('common.cancel')}
                          </button>
                        </div>
                      </div>
                    )}
                  </article>
                )
              })}
              </div>
              <CatalogPagination page={result.page} totalPages={result.totalPages} onPageChange={changePage} />
            </>
          ) : null}
        </section>
      </Container>
    </div>
  )
}

export default StaffApplicationsPage
