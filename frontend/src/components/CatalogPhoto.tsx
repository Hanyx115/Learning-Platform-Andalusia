import careerDevelopmentPhoto from '../assets/catalog/career-development.jpg'
import computerEngineeringPhoto from '../assets/catalog/computer-engineering.jpg'
import healthcareTrainingPhoto from '../assets/catalog/healthcare-training.jpg'
import { resolveApiUrl } from '../api/client'

interface CatalogPhotoProps {
  alt?: string
  categoryName?: string
  className?: string
  slug?: string
  imageUrl?: string | null
}

function getCatalogPhoto(categoryName = '', slug = '') {
  const catalogContext = `${categoryName} ${slug}`.toLowerCase()

  if (/software|computer|network|embedded|cyber|programming/.test(catalogContext)) {
    return computerEngineeringPhoto
  }

  if (/leadership|leader|quality|career|business|management|marketing|project|sales|entrepreneur|human resources|financial/.test(catalogContext)) {
    return careerDevelopmentPhoto
  }

  return healthcareTrainingPhoto
}

function CatalogPhoto({ alt = '', categoryName, className, imageUrl, slug }: CatalogPhotoProps) {
  return (
    <img
      className={className}
      src={imageUrl ? resolveApiUrl(imageUrl) : getCatalogPhoto(categoryName, slug)}
      alt={alt}
      width="1200"
      height="800"
      loading="lazy"
    />
  )
}

export default CatalogPhoto
