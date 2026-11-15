import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import {
  approveCompensationPayout,
  createInstructorPayout,
  createStaffPayout,
  getCompensationPayouts,
  getInstructorCompensationTargets,
  getStaffCompensationRates,
  reviewCompensationPayout,
  setInstructorCourseRate,
  setStaffMonthlyRate,
  type CompensationPayout,
  type InstructorCompensationTarget,
  type StaffCompensationRate,
} from '../../api/compensation'
import { useAuthProfile } from '../../auth/profileContext'

function AdminCompensationPage() {
  const { t, i18n } = useTranslation()
  const { profile } = useAuthProfile()
  const isAdmin = profile?.roles.includes('Admin') ?? false
  const isManager = profile?.roles.includes('Manager') ?? false
  const [targets, setTargets] = useState<InstructorCompensationTarget[]>([])
  const [staffRates, setStaffRates] = useState<StaffCompensationRate[]>([])
  const [payouts, setPayouts] = useState<CompensationPayout[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [workingKey, setWorkingKey] = useState('')
  const [error, setError] = useState('')

  const load = useCallback(async () => {
    const [loadedTargets, loadedPayouts, loadedStaffRates] = await Promise.all([
      getInstructorCompensationTargets(),
      getCompensationPayouts(),
      getStaffCompensationRates(),
    ])
    setTargets(loadedTargets)
    setPayouts(loadedPayouts)
    setStaffRates(loadedStaffRates)
  }, [])

  useEffect(() => {
    let isCurrent = true
    Promise.all([
      getInstructorCompensationTargets(),
      getCompensationPayouts(),
      getStaffCompensationRates(),
    ]).then(([loadedTargets, loadedPayouts, loadedStaffRates]) => {
      if (!isCurrent) return
      setTargets(loadedTargets)
      setPayouts(loadedPayouts)
      setStaffRates(loadedStaffRates)
    }).catch((requestError: unknown) => {
      if (isCurrent) setError(getApiErrorMessage(requestError, t('compensation.errors.load')))
    }).finally(() => {
      if (isCurrent) setIsLoading(false)
    })
    return () => { isCurrent = false }
  }, [t])

  async function run(key: string, action: () => Promise<unknown>) {
    setWorkingKey(key)
    setError('')
    try {
      await action()
      await load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('compensation.errors.action')))
    } finally {
      setWorkingKey('')
    }
  }

  function saveInstructorRate(event: FormEvent<HTMLFormElement>, target: InstructorCompensationTarget) {
    event.preventDefault()
    const amount = Number(new FormData(event.currentTarget).get('amount'))
    void run(`rate-${target.courseOfferingId}-${target.instructorUserId}`, () => setInstructorCourseRate(target, amount))
  }

  function saveStaffRate(event: FormEvent<HTMLFormElement>, rate: StaffCompensationRate) {
    event.preventDefault()
    const amount = Number(new FormData(event.currentTarget).get('amount'))
    void run(`staff-${rate.userId}`, () => setStaffMonthlyRate(rate.userId, amount))
  }

  const now = new Date()

  return (
    <section className="dashboard-section">
      <header className="dashboard-section__heading">
        <h1>{t('compensation.title')}</h1>
        <p>{t('compensation.description')}</p>
      </header>
      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {isLoading ? <p role="status">{t('common.loading')}</p> : null}

      <div className="operations-subheading">
        <h2>{t('compensation.instructors.title')}</h2>
        <p>{t('compensation.instructors.description')}</p>
      </div>
      <div className="dashboard-list">
        {targets.map((target) => {
          const key = `${target.courseOfferingId}-${target.instructorUserId}`
          return (
            <article className="dashboard-card" key={key}>
              <div className="dashboard-card__heading">
                <div><h2>{target.courseTitle}</h2><p>{target.instructorName}</p></div>
                {target.payoutStatus ? <span className="status-badge status-badge--neutral">{target.payoutStatus}</span> : null}
              </div>
              <p>{new Date(target.startsAtUtc).toLocaleDateString(i18n.language)} – {new Date(target.endsAtUtc).toLocaleDateString(i18n.language)}</p>
              {isManager ? <form className="compensation-rate-form" onSubmit={(event) => saveInstructorRate(event, target)}>
                <label>{t('compensation.rate')}<input name="amount" type="number" min="0.01" step="0.01" defaultValue={target.amount ?? ''} required /></label>
                <span>EGP</span>
                <button className="button button--secondary" disabled={workingKey === `rate-${key}` || Boolean(target.payoutStatus)}>{t('common.save')}</button>
              </form> : <p>{t('compensation.rate')}: {target.amount?.toLocaleString(i18n.language) ?? '—'} EGP</p>}
              {isManager && target.rateId && target.isEligibleForPayout && !target.payoutStatus ? (
                <button className="button button--primary" type="button" disabled={Boolean(workingKey)} onClick={() => void run(`payout-${target.rateId}`, () => createInstructorPayout(target.rateId!))}>
                  {t('compensation.createPayout')}
                </button>
              ) : null}
            </article>
          )
        })}
      </div>

      <>
        <div className="operations-subheading"><h2>{t('compensation.staff.title')}</h2><p>{t('compensation.staff.description')}</p></div>
        <div className="dashboard-list">
          {staffRates.map((rate) => (
            <article className="dashboard-card" key={rate.userId}>
              <div className="dashboard-card__heading"><div><h2>{rate.staffName}</h2><p>{rate.role}</p></div></div>
              {isAdmin ? <form className="compensation-rate-form" onSubmit={(event) => saveStaffRate(event, rate)}>
                <label>{t('compensation.monthlyRate')}<input name="amount" type="number" min="0.01" step="0.01" defaultValue={rate.monthlyAmount ?? ''} required /></label>
                <span>EGP</span>
                <button className="button button--secondary" disabled={workingKey === `staff-${rate.userId}`}>{t('common.save')}</button>
              </form> : <p>{t('compensation.monthlyRate')}: {rate.monthlyAmount?.toLocaleString(i18n.language) ?? '—'} EGP</p>}
              {isManager && rate.monthlyAmount ? <button className="button button--primary" type="button" disabled={Boolean(workingKey)} onClick={() => void run(`staff-payout-${rate.userId}`, () => createStaffPayout(rate.userId, now.getFullYear(), now.getMonth() + 1))}>{t('compensation.createMonthlyPayout')}</button> : null}
            </article>
          ))}
        </div>
      </>

      <div className="operations-subheading"><h2>{t('compensation.payouts.title')}</h2><p>{t('compensation.payouts.description')}</p></div>
      <div className="dashboard-list">
        {payouts.map((payout) => (
          <article className="dashboard-card" key={payout.id}>
            <div className="dashboard-card__heading"><div><h2>{payout.payeeName}</h2><p>{payout.description}</p></div><span className="status-badge status-badge--neutral">{payout.status}</span></div>
            <strong>{payout.amount.toLocaleString(i18n.language)} {payout.currency}</strong>
            <div className="dashboard-card__actions">
              {isManager && payout.status === 'Draft' ? <button className="button button--secondary" type="button" disabled={Boolean(workingKey)} onClick={() => void run(`review-${payout.id}`, () => reviewCompensationPayout(payout.id))}>{t('compensation.review')}</button> : null}
              {isAdmin && payout.status === 'Reviewed' ? <button className="button button--primary" type="button" disabled={Boolean(workingKey)} onClick={() => void run(`approve-${payout.id}`, () => approveCompensationPayout(payout.id))}>{t('compensation.approve')}</button> : null}
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

export default AdminCompensationPage
