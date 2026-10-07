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

function CompanyCard({ company }: { company: Company }) {
  const CompanyIcon = company.Icon

  return (
    <li className="company-carousel__item">
      <span className="company-carousel__logo" aria-hidden="true">
        {CompanyIcon ? <CompanyIcon color={company.color} /> : <img src={company.imageSource} alt="" />}
      </span>
      <span className="company-carousel__name">{company.name}</span>
    </li>
  )
}

function ForBusinessPage() {
  const { t } = useTranslation()
  const highlights = t('forBusiness.highlights', { returnObjects: true }) as string[]

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

          <div className="company-carousel">
            <div className="company-carousel__track">
              <ul className="company-carousel__group">
                {companies.map((company) => (
                  <CompanyCard key={company.name} company={company} />
                ))}
              </ul>
              <ul className="company-carousel__group" aria-hidden="true">
                {companies.map((company) => (
                  <CompanyCard key={`duplicate-${company.name}`} company={company} />
                ))}
              </ul>
            </div>
          </div>
        </Container>
      </section>
    </div>
  )
}

export default ForBusinessPage
