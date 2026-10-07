import { useTranslation } from 'react-i18next'
import { useCourses } from '../../../api/catalogQueries'
import Card from '../../../components/Card'
import Section from '../../../components/Section'

function PopularCategories() {
  const { t } = useTranslation()
  const query = useCourses({ page: 1, pageSize: 100, status: 'Published', sort: 'title' })
  const categoryCounts = new Map<string, number>()

  query.data?.items.forEach((course) => {
    categoryCounts.set(course.categoryName, (categoryCounts.get(course.categoryName) ?? 0) + 1)
  })

  const items = [...categoryCounts.entries()]
    .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))
    .slice(0, 4)

  return (
    <Section
      className="categories"
      eyebrowKey="home.categories.eyebrow"
      titleKey="home.categories.title"
      descriptionKey="home.categories.description"
    >
      {query.isPending ? <p className="home-query-state">{t('common.loading')}</p> : null}
      {query.isError ? <p className="home-query-state" role="alert">{t('common.error')}</p> : null}
      {query.isSuccess ? (
        <div className="card-grid card-grid--compact">
          {items.map(([name, count]) => (
            <Card key={name} className="category-card">
              <span className="icon-circle" aria-hidden="true">
                {name.charAt(0)}
              </span>
              <h3 dir="auto">{name}</h3>
              <p>{t('home.categories.courseCount', { count })}</p>
            </Card>
          ))}
        </div>
      ) : null}
    </Section>
  )
}

export default PopularCategories
