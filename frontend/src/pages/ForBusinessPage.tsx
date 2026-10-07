import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { FaAmazon, FaApple, FaGoogle, FaMeta, FaMicrosoft } from 'react-icons/fa6'
import { SiAnthropic, SiNetflix, SiNvidia } from 'react-icons/si'
import type { IconType } from 'react-icons'
import openAiLogo from '@lobehub/icons-static-svg/icons/openai.svg'
import Container from '../components/Container'
import './ForBusinessPage.css'
import './PublicPages.css'

interface Company {
  name: string
  color: string
  Icon?: IconType
  imageSource?: string
}

const companies: Company[] = [
  { name: 'Meta', color: '#0866ff', Icon: FaMeta },
  { name: 'Apple', color: '#000000', Icon: FaApple },
  { name: 'Amazon', color: '#ff9900', Icon: FaAmazon },
  { name: 'Netflix', color: '#e50914', Icon: SiNetflix },
  { name: 'Google', color: '#4285f4', Icon: FaGoogle },
  { name: 'Microsoft', color: '#f25022', Icon: FaMicrosoft },
  { name: 'Anthropic', color: '#d97757', Icon: SiAnthropic },
  { name: 'NVIDIA', color: '#76b900', Icon: SiNvidia },
  { name: 'OpenAI', color: '#000000', imageSource: openAiLogo },
]

function ForBusinessPage() {
  const { t } = useTranslation()
  const [activeIndex, setActiveIndex] = useState(0)
  const [isPaused, setIsPaused] = useState(false)
  const highlights = t('forBusiness.highlights', { returnObjects: true }) as string[]
  const activeCompany = companies[activeIndex]
  const ActiveCompanyIcon = activeCompany.Icon

  const showPrevious = () => {
    setActiveIndex((currentIndex) => (currentIndex - 1 + companies.length) % companies.length)
  }

  const showNext = () => {
    setActiveIndex((currentIndex) => (currentIndex + 1) % companies.length)
  }

  useEffect(() => {
    const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false

    if (isPaused || prefersReducedMotion) {
      return undefined
    }

    const intervalId = window.setInterval(() => {
      setActiveIndex((currentIndex) => (currentIndex + 1) % companies.length)
    }, 4000)
    return () => window.clearInterval(intervalId)
  }, [isPaused])

  return (
    <div className="public-page for-business-page">
      <section className="public-page__hero">
        <Container>
          <p className="public-page__eyebrow">{t('forBusiness.eyebrow')}</p>
          <h1>{t('forBusiness.title')}</h1>
          <p className="public-page__lead">{t('forBusiness.description')}</p>
        </Container>
      </section>

      <section className="public-page__section">
        <Container className="for-business-page__intro">
          <article className="public-page__panel">
            <h2>{t('forBusiness.trainingTitle')}</h2>
            <ul className="public-page__list">
              {highlights.map((highlight) => (
                <li key={highlight}>{highlight}</li>
              ))}
            </ul>
            <Link className="button button--primary for-business-page__cta" to="/contact">
              {t('forBusiness.contactCta')}
            </Link>
          </article>

          <article className="public-page__panel">
            <h2>{t('forBusiness.outcomesTitle')}</h2>
            <p>{t('forBusiness.outcomesDescription')}</p>
          </article>
        </Container>
      </section>

      <section
        className="for-business-page__companies"
        aria-labelledby="company-carousel-title"
        aria-roledescription="carousel"
      >
        <Container>
          <header className="for-business-page__companies-header">
            <p className="public-page__eyebrow">{t('forBusiness.companiesEyebrow')}</p>
            <h2 id="company-carousel-title">{t('forBusiness.companiesTitle')}</h2>
            <p>{t('forBusiness.companiesDescription')}</p>
          </header>

          <div
            className="company-carousel"
            onMouseEnter={() => setIsPaused(true)}
            onMouseLeave={() => setIsPaused(false)}
            onFocusCapture={() => setIsPaused(true)}
            onBlurCapture={(event) => {
              if (!event.currentTarget.contains(event.relatedTarget)) {
                setIsPaused(false)
              }
            }}
          >
            <button
              className="company-carousel__control"
              type="button"
              aria-label={t('forBusiness.previousCompany')}
              onClick={showPrevious}
            >
              <span aria-hidden="true">‹</span>
            </button>

            <div className="company-carousel__viewport" aria-live={isPaused ? 'polite' : 'off'}>
              <article className="company-carousel__slide">
                <span className="company-carousel__monogram" aria-hidden="true">
                  {ActiveCompanyIcon ? (
                    <ActiveCompanyIcon color={activeCompany.color} />
                  ) : (
                    <img src={activeCompany.imageSource} alt="" />
                  )}
                </span>
                <p className="company-carousel__name">{activeCompany.name}</p>
                <p className="company-carousel__position">
                  {t('forBusiness.carouselPosition', {
                    current: activeIndex + 1,
                    total: companies.length,
                  })}
                </p>
              </article>
            </div>

            <button
              className="company-carousel__control"
              type="button"
              aria-label={t('forBusiness.nextCompany')}
              onClick={showNext}
            >
              <span aria-hidden="true">›</span>
            </button>
          </div>

          <div className="company-carousel__dots" aria-hidden="true">
            {companies.map((company, index) => (
              <span
                key={company.name}
                className={index === activeIndex ? 'company-carousel__dot company-carousel__dot--active' : 'company-carousel__dot'}
              />
            ))}
          </div>

        </Container>
      </section>
    </div>
  )
}

export default ForBusinessPage
