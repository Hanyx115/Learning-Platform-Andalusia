import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuthProfile } from '../auth/profileContext'
import './LearnerCompanion.css'

const INACTIVITY_DELAY_MS = 15 * 60 * 1000

const petEmoji = {
  cat: '🐱',
  owl: '🦉',
  fox: '🦊',
} as const

function LearnerCompanion() {
  const { profile } = useAuthProfile()
  const { t } = useTranslation()
  const [isMessageVisible, setIsMessageVisible] = useState(false)
  const timeoutRef = useRef<number | null>(null)

  const isEnabled = Boolean(profile?.roles.includes('Learner') && profile.learnerPetEnabled)
  const petType = profile?.learnerPetType ?? 'cat'

  useEffect(() => {
    if (!isEnabled) {
      return
    }

    const scheduleReminder = () => {
      if (timeoutRef.current !== null) {
        window.clearTimeout(timeoutRef.current)
      }
      timeoutRef.current = window.setTimeout(() => setIsMessageVisible(true), INACTIVITY_DELAY_MS)
    }

    const handleActivity = (event: Event) => {
      if (event.target instanceof Element && event.target.closest('.learner-companion')) {
        return
      }
      setIsMessageVisible(false)
      scheduleReminder()
    }

    const activityEvents = ['pointerdown', 'keydown', 'scroll', 'touchstart'] as const
    activityEvents.forEach((eventName) => window.addEventListener(eventName, handleActivity, { passive: true }))
    scheduleReminder()

    return () => {
      activityEvents.forEach((eventName) => window.removeEventListener(eventName, handleActivity))
      if (timeoutRef.current !== null) {
        window.clearTimeout(timeoutRef.current)
      }
    }
  }, [isEnabled])

  if (!isEnabled) {
    return null
  }

  return (
    <aside className="learner-companion" aria-label={t('learnerPet.companionLabel')}>
      {isMessageVisible ? (
        <div className="learner-companion__message" role="status" aria-live="polite">
          <strong>{t('learnerPet.helpTitle')}</strong>
          <p>{t('learnerPet.inactiveMessage')}</p>
          <button type="button" onClick={() => setIsMessageVisible(false)}>
            {t('learnerPet.backButton')}
          </button>
        </div>
      ) : null}
      <button
        className="learner-companion__pet"
        type="button"
        aria-label={t('learnerPet.petButton', { pet: t(`learnerPet.types.${petType}`) })}
        onClick={() => setIsMessageVisible((visible) => !visible)}
      >
        <span aria-hidden="true">{petEmoji[petType]}</span>
      </button>
    </aside>
  )
}

export default LearnerCompanion
