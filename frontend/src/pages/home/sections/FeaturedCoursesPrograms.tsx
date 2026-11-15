import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useCourses, usePrograms } from '../../../api/catalogQueries'
import Card from '../../../components/Card'
import CatalogPhoto from '../../../components/CatalogPhoto'
import Section from '../../../components/Section'

function FeaturedCoursesPrograms() {
  const { t } = useTranslation()
  const coursesQuery = useCourses({ page: 1, pageSize: 2, status: 'Published', sort: 'newest' })
  const programsQuery = usePrograms({ page: 1, pageSize: 2, status: 'Published' })
  const courses = coursesQuery.data?.items ?? []
  const programs = programsQuery.data?.items ?? []
  const isPending = coursesQuery.isPending || programsQuery.isPending
  const isError = coursesQuery.isError || programsQuery.isError

  return (
    <Section
      contentKey="featured"
      className="featured"
      eyebrowKey="home.featured.eyebrow"
      titleKey="home.featured.title"
      descriptionKey="home.featured.description"
    >
      {isPending ? <p className="home-query-state">{t('common.loading')}</p> : null}
      {isError ? <p className="home-query-state" role="alert">{t('common.error')}</p> : null}
      {!isPending && !isError ? (
        <div className="card-grid">
          {courses.map((course) => (
            <Card key={course.id}>
              <CatalogPhoto
                imageUrl={course.imageUrl}
                categoryName={course.categoryName}
                slug={course.slug}
                className="home-card-photo"
              />
              <span className="badge">{t('home.featured.courseLabel')}</span>
              <h3 dir="auto"><Link to={`/courses/${course.slug}`}>{course.title}</Link></h3>
              <p dir="auto">{course.categoryName} · {course.type}</p>
            </Card>
          ))}
          {programs.map((program) => (
            <Card key={program.id}>
              <CatalogPhoto imageUrl={program.imageUrl} slug={program.slug} className="home-card-photo" />
              <span className="badge">{t('home.featured.programLabel')}</span>
              <h3 dir="auto"><Link to={`/programs/${program.slug}`}>{program.title}</Link></h3>
              <p dir="auto">{program.description}</p>
            </Card>
          ))}
        </div>
      ) : null}
      <Link to="/courses" className="section-link">
        {t('home.featured.viewAll')}
      </Link>
    </Section>
  )
}

export default FeaturedCoursesPrograms
