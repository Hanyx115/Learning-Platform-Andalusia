import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { useCourses } from '../../api/catalogQueries'
import Card from '../../components/Card'
import CatalogPhoto from '../../components/CatalogPhoto'
import Container from '../../components/Container'
import { CatalogHero, CatalogMeta, CatalogPagination, QueryState } from './CatalogUi'
import './catalog.css'

const PAGE_SIZE = 6
const allowedSorts = ['title', 'title-desc', 'newest', 'oldest'] as const
type CourseSort = (typeof allowedSorts)[number]

function parsePage(value: string | null) {
  const page = Number(value)
  return Number.isInteger(page) && page > 0 ? page : 1
}

function parseSort(value: string | null): CourseSort {
  return allowedSorts.includes(value as CourseSort) ? (value as CourseSort) : 'title'
}

function CoursesPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = parsePage(searchParams.get('page'))
  const search = searchParams.get('search') ?? ''
  const category = searchParams.get('category') ?? ''
  const type = searchParams.get('type') ?? ''
  const sort = parseSort(searchParams.get('sort'))

  const coursesQuery = useCourses({
    page,
    pageSize: PAGE_SIZE,
    search,
    category,
    type,
    sort,
    status: 'Published',
  })
  const optionsQuery = useCourses({ page: 1, pageSize: 100, status: 'Published', sort: 'title' })

  const categories = [...new Set(optionsQuery.data?.items.map((course) => course.categoryName) ?? [])]
  const types = [...new Set(optionsQuery.data?.items.map((course) => course.type) ?? [])]

  function applyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const next = new URLSearchParams()

    for (const key of ['search', 'category', 'type', 'sort']) {
      const value = String(data.get(key) ?? '').trim()
      if (value && !(key === 'sort' && value === 'title')) {
        next.set(key, value)
      }
    }

    setSearchParams(next)
  }

  function changePage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    if (nextPage === 1) {
      next.delete('page')
    } else {
      next.set('page', String(nextPage))
    }
    setSearchParams(next)
  }

  return (
    <>
      <CatalogHero
        eyebrow={t('courses.eyebrow')}
        title={t('courses.title')}
        description={t('courses.description')}
      />
      <section className="catalog-page">
        <Container>
          <form className="catalog-filters" onSubmit={applyFilters}>
            <label>
              {t('courses.searchLabel')}
              <input name="search" type="search" defaultValue={search} placeholder={t('courses.searchPlaceholder')} />
            </label>
            <label>
              {t('courses.categoryLabel')}
              <select name="category" defaultValue={category}>
                <option value="">{t('courses.allCategories')}</option>
                {categories.map((item) => <option key={item} value={item}>{item}</option>)}
              </select>
            </label>
            <label>
              {t('courses.typeLabel')}
              <select name="type" defaultValue={type}>
                <option value="">{t('courses.allTypes')}</option>
                {types.map((item) => <option key={item} value={item}>{item}</option>)}
              </select>
            </label>
            <label>
              {t('courses.sortLabel')}
              <select name="sort" defaultValue={sort}>
                <option value="title">{t('courses.sortTitle')}</option>
                <option value="title-desc">{t('courses.sortTitleDescending')}</option>
                <option value="newest">{t('courses.sortNewest')}</option>
                <option value="oldest">{t('courses.sortOldest')}</option>
              </select>
            </label>
            <div className="catalog-filters__actions">
              <button className="button button--primary" type="submit">{t('courses.applyFilters')}</button>
              <Link className="button button--secondary" to="/courses">{t('courses.resetFilters')}</Link>
            </div>
          </form>

          {coursesQuery.isPending ? <QueryState kind="loading" /> : null}
          {coursesQuery.isError ? <QueryState kind="error" onRetry={() => void coursesQuery.refetch()} /> : null}
          {coursesQuery.data && coursesQuery.data.items.length === 0 ? <QueryState kind="empty" /> : null}

          {coursesQuery.data && coursesQuery.data.items.length > 0 ? (
            <>
              <p className="catalog-results">{t('courses.results', { count: coursesQuery.data.totalCount })}</p>
              <div className="catalog-grid">
                {coursesQuery.data.items.map((course) => (
                  <Card key={course.id} as="article" className="catalog-card">
                    <CatalogPhoto
                      categoryName={course.categoryName}
                      slug={course.slug}
                      className="catalog-card__image"
                    />
                    <div className="catalog-card__meta">
                      <CatalogMeta>{course.categoryName}</CatalogMeta>
                      <CatalogMeta>{course.type}</CatalogMeta>
                    </div>
                    <h2 dir="auto"><Link to={`/courses/${course.slug}`}>{course.title}</Link></h2>
                    <p dir="auto">{course.description}</p>
                    <Link className="catalog-card__link" to={`/courses/${course.slug}`}>{t('common.viewDetails')}</Link>
                  </Card>
                ))}
              </div>
              <CatalogPagination
                page={page}
                totalPages={coursesQuery.data.totalPages}
                onPageChange={changePage}
              />
            </>
          ) : null}
        </Container>
      </section>
    </>
  )
}

export default CoursesPage
