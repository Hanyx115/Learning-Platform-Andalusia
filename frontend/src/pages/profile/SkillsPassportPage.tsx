import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getApiErrorMessage } from '../../api/auth'
import { getMySkillsPassport, type SkillsPassport } from '../../api/skills'

function SkillsPassportPage() {
  const { t, i18n } = useTranslation()
  const [passport, setPassport] = useState<SkillsPassport | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    getMySkillsPassport()
      .then((data) => active && setPassport(data))
      .catch((requestError: unknown) => active && setError(getApiErrorMessage(requestError, t('skillsPassport.errors.load'))))
    return () => { active = false }
  }, [t])

  return (
    <section className="dashboard-section skills-passport">
      <div className="dashboard-section__heading">
        <span className="page-eyebrow">{t('skillsPassport.eyebrow')}</span>
        <h1>{t('skillsPassport.title')}</h1>
        <p>{t('skillsPassport.description')}</p>
      </div>

      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {!passport && !error ? <p role="status">{t('skillsPassport.loading')}</p> : null}

      {passport ? (
        <>
          <div className="skills-passport__summary" aria-label={t('skillsPassport.summaryLabel')}>
            <article><strong>{passport.assessedSkills}</strong><span>{t('skillsPassport.assessed')}</span></article>
            <article><strong>{passport.demonstratedSkills}</strong><span>{t('skillsPassport.demonstrated')}</span></article>
            <article><strong>{passport.developingSkills}</strong><span>{t('skillsPassport.developing')}</span></article>
          </div>

          {passport.skills.length === 0 ? (
            <div className="dashboard-card skills-passport__empty">
              <h2>{t('skillsPassport.emptyTitle')}</h2>
              <p>{t('skillsPassport.empty')}</p>
            </div>
          ) : (
            <div className="dashboard-list">
              {passport.skills.map((skill) => (
                <article className="dashboard-card skill-card" key={skill.skillId}>
                  <div className="dashboard-card__heading">
                    <div>
                      <span className="skill-card__category">{skill.category}</span>
                      <h2 dir="auto">{skill.name}</h2>
                    </div>
                    <span className={`status-badge status-badge--${skill.status === 'Demonstrated' ? 'positive' : 'neutral'}`}>
                      {t(`skillsPassport.status.${skill.status}`)}
                    </span>
                  </div>
                  <p>{skill.description}</p>
                  <div className="skill-card__meter-row">
                    <div className="skill-card__meter" role="progressbar" aria-valuemin={0} aria-valuemax={100} aria-valuenow={skill.masteryPercentage ?? 0}>
                      <span style={{ width: `${skill.masteryPercentage ?? 0}%` }} />
                    </div>
                    <strong>{skill.masteryPercentage === null ? '—' : `${skill.masteryPercentage}%`}</strong>
                  </div>
                  <p className="skill-card__evidence-count">
                    {t('skillsPassport.evidenceCount', { count: skill.gradedEvidenceCount })}
                  </p>
                  {skill.evidence.length > 0 ? (
                    <details className="skill-card__evidence">
                      <summary>{t('skillsPassport.viewEvidence')}</summary>
                      {skill.evidence.map((item) => (
                        <div key={`${item.offeringType}-${item.offeringId}`}>
                          <strong dir="auto">{item.learningTitle}</strong>
                          <span>{t('skillsPassport.evidenceScore', { score: item.averageScorePercentage, count: item.gradedAssignments })}</span>
                          {item.lastGradedAtUtc ? <time dateTime={item.lastGradedAtUtc}>{new Date(item.lastGradedAtUtc).toLocaleDateString(i18n.language)}</time> : null}
                        </div>
                      ))}
                    </details>
                  ) : null}
                </article>
              ))}
            </div>
          )}
          <p className="skills-passport__note">{t('skillsPassport.evidenceNote')}</p>
        </>
      ) : null}
    </section>
  )
}

export default SkillsPassportPage
