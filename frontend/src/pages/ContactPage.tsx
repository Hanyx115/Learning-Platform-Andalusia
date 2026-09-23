import { useTranslation } from 'react-i18next'
import Container from '../components/Container'
import './PublicPages.css'

function ContactPage() {
  const { t } = useTranslation()

  return (
    <div className="public-page">
      <section className="public-page__hero">
        <Container>
          <p className="public-page__eyebrow">{t('contact.eyebrow')}</p>
          <h1>{t('contact.title')}</h1>
          <p className="public-page__lead">{t('contact.description')}</p>
        </Container>
      </section>

      <section className="public-page__section">
        <Container className="contact-layout">
          <div className="contact-details" aria-label={t('contact.detailsLabel')}>
            <article className="contact-card">
              <h2>{t('contact.phoneLabel')}</h2>
              <a href="tel:+201202317780" dir="ltr">
                {t('contact.phoneValue')}
              </a>
            </article>
            <article className="contact-card">
              <h2>{t('contact.emailLabel')}</h2>
              <a href="mailto:andalusiaacadem@andalusia.com">{t('contact.emailValue')}</a>
            </article>
            <article className="contact-card">
              <h2>{t('contact.addressLabel')}</h2>
              <address>{t('contact.addressValue')}</address>
            </article>
          </div>

          <form
            className="contact-form"
            action="mailto:andalusiaacadem@andalusia.com?subject=Website%20inquiry"
            method="post"
            encType="text/plain"
          >
            <h2>{t('contact.formTitle')}</h2>
            <p>{t('contact.formDescription')}</p>

            <div className="contact-form__row">
              <label>
                {t('contact.firstNameLabel')}
                <input name="firstName" autoComplete="given-name" required />
              </label>
              <label>
                {t('contact.lastNameLabel')}
                <input name="lastName" autoComplete="family-name" required />
              </label>
            </div>

            <label>
              {t('contact.formEmailLabel')}
              <input name="email" type="email" autoComplete="email" required />
            </label>

            <label>
              {t('contact.mobileLabel')}
              <input name="mobile" type="tel" autoComplete="tel" />
            </label>

            <label>
              {t('contact.messageLabel')}
              <textarea name="message" rows={5} required />
            </label>

            <button className="button button--primary" type="submit">
              {t('contact.submit')}
            </button>
          </form>
        </Container>
      </section>
    </div>
  )
}

export default ContactPage
