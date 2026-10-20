import { useTranslation } from 'react-i18next'
import type { Offering } from '../../api/catalog'

interface OfferingSelectorProps {
  offerings: Offering[]
  selectedId: string
  onSelect: (offeringId: string) => void
}

function OfferingSelector({ offerings, selectedId, onSelect }: OfferingSelectorProps) {
  const { i18n, t } = useTranslation()

  return (
    <fieldset className="offering-selector">
      <legend>{t('offerings.title')}</legend>
      <p>{t('offerings.description')}</p>
      <div className="offering-selector__list">
        {offerings.map((offering) => (
          <label
            key={offering.id}
            className={selectedId === offering.id ? 'offering-card offering-card--selected' : 'offering-card'}
          >
            <input
              type="radio"
              name="offering"
              value={offering.id}
              checked={selectedId === offering.id}
              onChange={() => onSelect(offering.id)}
            />
            <span>
              <strong>
                {t('offerings.dates', {
                  start: new Date(offering.startsAtUtc).toLocaleDateString(i18n.language),
                  end: new Date(offering.endsAtUtc).toLocaleDateString(i18n.language),
                })}
              </strong>
              <span>{t('offerings.price', { price: offering.price, currency: offering.currency })}</span>
              <span>{t('offerings.capacity', { capacity: offering.capacity })}</span>
              <span>
                {t('offerings.applyBy', {
                  date: new Date(offering.applicationClosesAtUtc).toLocaleDateString(i18n.language),
                })}
              </span>
            </span>
          </label>
        ))}
      </div>
    </fieldset>
  )
}

export default OfferingSelector
