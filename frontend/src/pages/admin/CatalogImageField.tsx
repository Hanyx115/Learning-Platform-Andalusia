import { useId, useState, type ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { uploadCatalogImage } from '../../api/admin'
import { getApiErrorMessage } from '../../api/auth'
import { resolveApiUrl } from '../../api/client'

interface CatalogImageFieldProps {
  imageUrl: string
  onChange: (imageUrl: string) => void
}

function CatalogImageField({ imageUrl, onChange }: CatalogImageFieldProps) {
  const { t } = useTranslation()
  const inputId = useId()
  const [isUploading, setIsUploading] = useState(false)
  const [error, setError] = useState('')

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.currentTarget.files?.[0]
    event.currentTarget.value = ''
    if (!file) {
      return
    }

    setError('')
    setIsUploading(true)
    try {
      const uploaded = await uploadCatalogImage(file)
      onChange(uploaded.imageUrl)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('adminCatalog.fields.imageUploadError')))
    } finally {
      setIsUploading(false)
    }
  }

  return (
    <fieldset className="catalog-image-field">
      <legend>{t('adminCatalog.fields.image')}</legend>
      <p>{t('adminCatalog.fields.imageHint')}</p>
      {imageUrl ? (
        <img src={resolveApiUrl(imageUrl)} alt={t('adminCatalog.fields.imagePreview')} />
      ) : (
        <div className="catalog-image-field__placeholder" aria-hidden="true">+</div>
      )}
      <div className="dashboard-card__actions">
        <label className="button button--secondary" htmlFor={inputId}>
          {isUploading ? t('adminCatalog.fields.uploadingImage') : t('adminCatalog.fields.chooseImage')}
        </label>
        <input
          id={inputId}
          className="catalog-image-field__input"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          disabled={isUploading}
          onChange={(event) => void handleFileChange(event)}
        />
        {imageUrl ? (
          <button className="button button--secondary" type="button" onClick={() => onChange('')}>
            {t('adminCatalog.fields.removeImage')}
          </button>
        ) : null}
      </div>
      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
    </fieldset>
  )
}

export default CatalogImageField
