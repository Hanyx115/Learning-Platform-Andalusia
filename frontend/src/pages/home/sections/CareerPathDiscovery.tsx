import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useCareerPaths } from '../../../api/catalogQueries'
import Card from '../../../components/Card'
import CatalogPhoto from '../../../components/CatalogPhoto'
import Section from '../../../components/Section'

function CareerPathDiscovery() {
  const { t } = useTranslation()
  const query = useCareerPaths()
  const items = query.data?.filter((item) => item.status === 'Published').slice(0, 3) ?? []

  return (
    <Section
      className="career-paths"
      eyebrowKey="home.careerPaths.eyebrow"
      titleKey="home.careerPaths.title"
      descriptionKey="home.careerPaths.description"
    >
      {query.isPending ? <p className="home-query-state">{t('common.loading')}</p> : null}
      {query.isError ? <p className="home-query-state" role="alert">{t('common.error')}</p> : null}
      {query.isSuccess ? (
        <div className="card-grid">
          {items.map((item) => (
            <Card key={item.id}>
              <CatalogPhoto slug={item.slug} className="home-card-photo" />
              <h3 dir="auto"><Link to={`/career-paths/${item.slug}`}>{item.title}</Link></h3>
              <p dir="auto">{item.description}</p>
            </Card>
          ))}
        </div>
      ) : null}
      <Link to="/career-paths" className="section-link">
        {t('home.careerPaths.viewAll')}
      </Link>
    </Section>
  )
}

export default CareerPathDiscovery
