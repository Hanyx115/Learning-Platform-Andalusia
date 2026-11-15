import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getAdminUsers, updateAdminUserRoles, updateAdminUserStatus, type AdminUser } from '../../api/admin'
import { getApiErrorMessage } from '../../api/auth'
import { useAuthProfile } from '../../auth/profileContext'

const manageableRoles = ['Learner', 'Instructor', 'ContentManager', 'Manager']

function AdminUsersPage() {
  const { t } = useTranslation()
  const { profile } = useAuthProfile()
  const [users, setUsers] = useState<AdminUser[] | null>(null)
  const [draftRoles, setDraftRoles] = useState<Record<string, string[]>>({})
  const [workingUserId, setWorkingUserId] = useState<string | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let isCurrent = true
    getAdminUsers()
      .then((loaded) => {
        if (!isCurrent) return
        setUsers(loaded)
        setDraftRoles(Object.fromEntries(loaded.map((user) => [
          user.id,
          user.roles.filter((role) => manageableRoles.includes(role)),
        ])))
      })
      .catch((requestError: unknown) => {
        if (isCurrent) setError(getApiErrorMessage(requestError, t('adminUsers.errors.load')))
      })
    return () => { isCurrent = false }
  }, [t])

  function replaceUser(updated: AdminUser) {
    setUsers((current) => current?.map((user) => user.id === updated.id ? updated : user) ?? null)
    setDraftRoles((current) => ({
      ...current,
      [updated.id]: updated.roles.filter((role) => manageableRoles.includes(role)),
    }))
  }

  function toggleRole(userId: string, role: string) {
    setDraftRoles((current) => {
      const roles = current[userId] ?? []
      return {
        ...current,
        [userId]: roles.includes(role) ? roles.filter((item) => item !== role) : [...roles, role],
      }
    })
  }

  async function saveRoles(userId: string) {
    setWorkingUserId(userId)
    setError('')
    try {
      replaceUser(await updateAdminUserRoles(userId, draftRoles[userId] ?? []))
    } catch (requestError: unknown) {
      setError(getApiErrorMessage(requestError, t('adminUsers.errors.update')))
    } finally {
      setWorkingUserId(null)
    }
  }

  async function setActive(user: AdminUser) {
    setWorkingUserId(user.id)
    setError('')
    try {
      replaceUser(await updateAdminUserStatus(user.id, !user.isActive))
    } catch (requestError: unknown) {
      setError(getApiErrorMessage(requestError, t('adminUsers.errors.update')))
    } finally {
      setWorkingUserId(null)
    }
  }

  return (
    <section className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('adminUsers.title')}</h1>
        <p>{t('adminUsers.description')}</p>
      </div>
      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {users === null ? (
        <p role="status">{t('adminUsers.loading')}</p>
      ) : (
        <div className="dashboard-list">
          {users.map((user) => {
            const privilegedRoles = user.roles.filter((role) => !manageableRoles.includes(role))
            return (
              <article key={user.id} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <div>
                    <h2 dir="auto">{user.firstName} {user.lastName}</h2>
                    <p>{user.email}</p>
                  </div>
                  <span className={`status-badge ${user.isActive ? 'status-badge--positive' : 'status-badge--negative'}`}>
                    {user.isActive ? t('adminUsers.active') : t('adminUsers.inactive')}
                  </span>
                </div>
                {privilegedRoles.length ? <p>{t('adminUsers.protectedRoles')}: {privilegedRoles.join(', ')}</p> : null}
                <fieldset className="admin-user-roles">
                  <legend>{t('adminUsers.manageableRoles')}</legend>
                  {manageableRoles.map((role) => (
                    <label key={role}>
                      <input
                        type="checkbox"
                        checked={(draftRoles[user.id] ?? []).includes(role)}
                        onChange={() => toggleRole(user.id, role)}
                      />
                      {t(`profile.roleNames.${role}`, { defaultValue: role })}
                    </label>
                  ))}
                </fieldset>
                <div className="dashboard-card__actions">
                  <button type="button" className="button button--secondary" disabled={workingUserId === user.id} onClick={() => void saveRoles(user.id)}>
                    {t('adminUsers.saveRoles')}
                  </button>
                  <button
                    type="button"
                    className="button button--secondary"
                    disabled={workingUserId === user.id || (profile?.id === user.id && user.isActive)}
                    onClick={() => void setActive(user)}
                  >
                    {user.isActive ? t('adminUsers.deactivate') : t('adminUsers.activate')}
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

export default AdminUsersPage
