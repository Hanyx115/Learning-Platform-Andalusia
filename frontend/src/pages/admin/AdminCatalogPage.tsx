import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  createCourse,
  createCourseCategory,
  createProgram,
  getCourseCategories,
  updateCourse,
  updateCourseCategory,
  updateProgram,
  type CourseCategory,
} from '../../api/admin'
import { getCourses, getPrograms, type Course, type ProgramSummary } from '../../api/catalog'
import Container from '../../components/Container'

const STATUSES = ['Draft', 'Published', 'Archived']
const COURSE_TYPES = ['Online', 'Offline', 'Hybrid']

interface CourseDraft {
  title: string
  categoryId: string
  description: string
  status: string
  type: string
  price: string
}

interface ProgramDraft {
  title: string
  description: string
  status: string
  price: string
}

const emptyCourseDraft: CourseDraft = {
  title: '',
  categoryId: '',
  description: '',
  status: 'Published',
  type: 'Online',
  price: '',
}

const emptyProgramDraft: ProgramDraft = { title: '', description: '', status: 'Published', price: '' }

async function fetchAllStatuses<T>(fetchByStatus: (status: string) => Promise<{ items: T[] }>) {
  const pages = await Promise.all(STATUSES.map((status) => fetchByStatus(status)))
  return pages.flatMap((page) => page.items)
}

function AdminCatalogPage() {
  const { t } = useTranslation()
  const [categories, setCategories] = useState<CourseCategory[] | null>(null)
  const [courses, setCourses] = useState<Course[] | null>(null)
  const [programs, setPrograms] = useState<ProgramSummary[] | null>(null)
  const [loadError, setLoadError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)

  const [categoryDraft, setCategoryDraft] = useState('')
  const [categoryError, setCategoryError] = useState('')
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null)
  const [editingCategoryName, setEditingCategoryName] = useState('')

  const [courseDraft, setCourseDraft] = useState<CourseDraft>(emptyCourseDraft)
  const [courseError, setCourseError] = useState('')
  const [editingCourseId, setEditingCourseId] = useState<string | null>(null)

  const [programDraft, setProgramDraft] = useState<ProgramDraft>(emptyProgramDraft)
  const [programError, setProgramError] = useState('')
  const [editingProgramId, setEditingProgramId] = useState<string | null>(null)

  const loadAll = useCallback(() => {
    Promise.all([
      getCourseCategories(),
      fetchAllStatuses((status) => getCourses({ status, pageSize: 100 })),
      fetchAllStatuses((status) => getPrograms({ status, pageSize: 100 })),
    ])
      .then(([loadedCategories, loadedCourses, loadedPrograms]) => {
        setCategories(loadedCategories)
        setCourses(loadedCourses)
        setPrograms(loadedPrograms)
      })
      .catch((requestError: unknown) => {
        if (getApiErrorStatus(requestError) === 403) {
          setIsForbidden(true)
        } else {
          setLoadError(getApiErrorMessage(requestError, t('adminCatalog.errors.load')))
        }
      })
  }, [t])

  useEffect(() => {
    loadAll()
  }, [loadAll])

  if (isForbidden) {
    return (
      <Container className="auth-state-page">
        <h1>{t('forbidden.title')}</h1>
        <p>{t('forbidden.description')}</p>
      </Container>
    )
  }

  async function handleCreateCategory() {
    if (!categoryDraft.trim()) {
      return
    }
    setCategoryError('')
    try {
      await createCourseCategory(categoryDraft.trim())
      setCategoryDraft('')
      loadAll()
    } catch (requestError) {
      setCategoryError(getApiErrorMessage(requestError, t('adminCatalog.errors.save')))
    }
  }

  async function handleSaveCategoryEdit(id: string) {
    setCategoryError('')
    try {
      await updateCourseCategory(id, editingCategoryName.trim())
      setEditingCategoryId(null)
      loadAll()
    } catch (requestError) {
      setCategoryError(getApiErrorMessage(requestError, t('adminCatalog.errors.save')))
    }
  }

  async function handleSaveCourse() {
    setCourseError('')
    const request = {
      title: courseDraft.title.trim(),
      categoryId: courseDraft.categoryId,
      description: courseDraft.description.trim(),
      status: courseDraft.status,
      type: courseDraft.type,
      price: courseDraft.price === '' ? null : Number(courseDraft.price),
    }

    try {
      if (editingCourseId) {
        await updateCourse(editingCourseId, request)
      } else {
        await createCourse(request)
      }
      setCourseDraft(emptyCourseDraft)
      setEditingCourseId(null)
      loadAll()
    } catch (requestError) {
      setCourseError(getApiErrorMessage(requestError, t('adminCatalog.errors.save')))
    }
  }

  function startEditCourse(course: Course) {
    setEditingCourseId(course.id)
    setCourseDraft({
      title: course.title,
      categoryId: categories?.find((category) => category.slug === course.categorySlug)?.id ?? '',
      description: course.description,
      status: course.status,
      type: course.type,
      price: course.price?.toString() ?? '',
    })
  }

  async function handleSaveProgram() {
    setProgramError('')
    const request = {
      title: programDraft.title.trim(),
      description: programDraft.description.trim(),
      status: programDraft.status,
      price: programDraft.price === '' ? null : Number(programDraft.price),
    }

    try {
      if (editingProgramId) {
        await updateProgram(editingProgramId, request)
      } else {
        await createProgram(request)
      }
      setProgramDraft(emptyProgramDraft)
      setEditingProgramId(null)
      loadAll()
    } catch (requestError) {
      setProgramError(getApiErrorMessage(requestError, t('adminCatalog.errors.save')))
    }
  }

  function startEditProgram(program: ProgramSummary) {
    setEditingProgramId(program.id)
    setProgramDraft({
      title: program.title,
      description: program.description,
      status: program.status,
      price: program.price?.toString() ?? '',
    })
  }

  return (
    <div className="dashboard-section">
      <div className="dashboard-section__heading">
        <h1>{t('adminCatalog.title')}</h1>
        <p>{t('adminCatalog.description')}</p>
      </div>

      {loadError ? <p className="auth-message auth-message--error" role="alert">{loadError}</p> : null}

      {/* Categories */}
      <section className="dashboard-section">
        <h2>{t('adminCatalog.categoriesTitle')}</h2>
        {categoryError ? <p className="auth-message auth-message--error" role="alert">{categoryError}</p> : null}
        {categories === null ? (
          <p role="status">{t('adminCatalog.loading')}</p>
        ) : (
          <div className="dashboard-list">
            {categories.map((category) => (
              <article key={category.id} className="dashboard-card">
                {editingCategoryId === category.id ? (
                  <div className="dashboard-card__actions">
                    <input
                      type="text"
                      value={editingCategoryName}
                      onChange={(event) => setEditingCategoryName(event.target.value)}
                    />
                    <button className="button button--primary" type="button" onClick={() => void handleSaveCategoryEdit(category.id)}>
                      {t('adminCatalog.save')}
                    </button>
                    <button className="button button--secondary" type="button" onClick={() => setEditingCategoryId(null)}>
                      {t('adminCatalog.cancel')}
                    </button>
                  </div>
                ) : (
                  <div className="dashboard-card__heading">
                    <h3 dir="auto">{category.name}</h3>
                    <button
                      className="button button--secondary"
                      type="button"
                      onClick={() => {
                        setEditingCategoryId(category.id)
                        setEditingCategoryName(category.name)
                      }}
                    >
                      {t('adminCatalog.edit')}
                    </button>
                  </div>
                )}
              </article>
            ))}
          </div>
        )}
        <div className="dashboard-card__actions">
          <input
            type="text"
            placeholder={t('adminCatalog.newCategoryPlaceholder')}
            value={categoryDraft}
            onChange={(event) => setCategoryDraft(event.target.value)}
          />
          <button className="button button--primary" type="button" onClick={() => void handleCreateCategory()}>
            {t('adminCatalog.addCategory')}
          </button>
        </div>
      </section>

      {/* Courses */}
      <section className="dashboard-section">
        <h2>{t('adminCatalog.coursesTitle')}</h2>
        {courseError ? <p className="auth-message auth-message--error" role="alert">{courseError}</p> : null}
        {courses === null ? (
          <p role="status">{t('adminCatalog.loading')}</p>
        ) : (
          <div className="dashboard-list">
            {courses.map((course) => (
              <article key={course.id} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h3 dir="auto">{course.title}</h3>
                  <span className="status-badge status-badge--neutral">{course.status}</span>
                </div>
                <div className="dashboard-card__meta">
                  <span>{course.categoryName}</span>
                  <span>{course.type}</span>
                  {course.price !== null ? <span>{course.price} EGP</span> : null}
                </div>
                <div className="dashboard-card__actions">
                  <button className="button button--secondary" type="button" onClick={() => startEditCourse(course)}>
                    {t('adminCatalog.edit')}
                  </button>
                  <Link className="button button--secondary" to={`/admin/courses/${course.id}/offerings`}>
                    {t('adminCatalog.manageOfferings')}
                  </Link>
                </div>
              </article>
            ))}
          </div>
        )}

        <form
          className="dashboard-card"
          onSubmit={(event) => {
            event.preventDefault()
            void handleSaveCourse()
          }}
        >
          <h3>{editingCourseId ? t('adminCatalog.editCourse') : t('adminCatalog.newCourse')}</h3>
          <label>
            {t('adminCatalog.fields.title')}
            <input
              type="text"
              required
              value={courseDraft.title}
              onChange={(event) => setCourseDraft((previous) => ({ ...previous, title: event.target.value }))}
            />
          </label>
          <label>
            {t('adminCatalog.fields.category')}
            <select
              required
              value={courseDraft.categoryId}
              onChange={(event) => setCourseDraft((previous) => ({ ...previous, categoryId: event.target.value }))}
            >
              <option value="">{t('adminCatalog.fields.selectCategory')}</option>
              {categories?.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            {t('adminCatalog.fields.description')}
            <textarea
              rows={2}
              required
              value={courseDraft.description}
              onChange={(event) => setCourseDraft((previous) => ({ ...previous, description: event.target.value }))}
            />
          </label>
          <label>
            {t('adminCatalog.fields.status')}
            <select
              value={courseDraft.status}
              onChange={(event) => setCourseDraft((previous) => ({ ...previous, status: event.target.value }))}
            >
              {STATUSES.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </label>
          <label>
            {t('adminCatalog.fields.type')}
            <select
              value={courseDraft.type}
              onChange={(event) => setCourseDraft((previous) => ({ ...previous, type: event.target.value }))}
            >
              {COURSE_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <label>
            {t('adminCatalog.fields.price')}
            <input
              type="number"
              min={0}
              step="0.01"
              value={courseDraft.price}
              onChange={(event) => setCourseDraft((previous) => ({ ...previous, price: event.target.value }))}
            />
          </label>
          <div className="dashboard-card__actions">
            <button className="button button--primary" type="submit">
              {editingCourseId ? t('adminCatalog.save') : t('adminCatalog.create')}
            </button>
            {editingCourseId ? (
              <button
                className="button button--secondary"
                type="button"
                onClick={() => {
                  setEditingCourseId(null)
                  setCourseDraft(emptyCourseDraft)
                }}
              >
                {t('adminCatalog.cancel')}
              </button>
            ) : null}
          </div>
        </form>
      </section>

      {/* Programs */}
      <section className="dashboard-section">
        <h2>{t('adminCatalog.programsTitle')}</h2>
        {programError ? <p className="auth-message auth-message--error" role="alert">{programError}</p> : null}
        {programs === null ? (
          <p role="status">{t('adminCatalog.loading')}</p>
        ) : (
          <div className="dashboard-list">
            {programs.map((program) => (
              <article key={program.id} className="dashboard-card">
                <div className="dashboard-card__heading">
                  <h3 dir="auto">{program.title}</h3>
                  <span className="status-badge status-badge--neutral">{program.status}</span>
                </div>
                <div className="dashboard-card__meta">
                  {program.price !== null ? <span>{program.price} EGP</span> : null}
                </div>
                <div className="dashboard-card__actions">
                  <button className="button button--secondary" type="button" onClick={() => startEditProgram(program)}>
                    {t('adminCatalog.edit')}
                  </button>
                  <Link className="button button--secondary" to={`/admin/programs/${program.id}/offerings`}>
                    {t('adminCatalog.manageOfferings')}
                  </Link>
                </div>
              </article>
            ))}
          </div>
        )}

        <form
          className="dashboard-card"
          onSubmit={(event) => {
            event.preventDefault()
            void handleSaveProgram()
          }}
        >
          <h3>{editingProgramId ? t('adminCatalog.editProgram') : t('adminCatalog.newProgram')}</h3>
          <label>
            {t('adminCatalog.fields.title')}
            <input
              type="text"
              required
              value={programDraft.title}
              onChange={(event) => setProgramDraft((previous) => ({ ...previous, title: event.target.value }))}
            />
          </label>
          <label>
            {t('adminCatalog.fields.description')}
            <textarea
              rows={2}
              required
              value={programDraft.description}
              onChange={(event) => setProgramDraft((previous) => ({ ...previous, description: event.target.value }))}
            />
          </label>
          <label>
            {t('adminCatalog.fields.status')}
            <select
              value={programDraft.status}
              onChange={(event) => setProgramDraft((previous) => ({ ...previous, status: event.target.value }))}
            >
              {STATUSES.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </label>
          <label>
            {t('adminCatalog.fields.price')}
            <input
              type="number"
              min={0}
              step="0.01"
              value={programDraft.price}
              onChange={(event) => setProgramDraft((previous) => ({ ...previous, price: event.target.value }))}
            />
          </label>
          <div className="dashboard-card__actions">
            <button className="button button--primary" type="submit">
              {editingProgramId ? t('adminCatalog.save') : t('adminCatalog.create')}
            </button>
            {editingProgramId ? (
              <button
                className="button button--secondary"
                type="button"
                onClick={() => {
                  setEditingProgramId(null)
                  setProgramDraft(emptyProgramDraft)
                }}
              >
                {t('adminCatalog.cancel')}
              </button>
            ) : null}
          </div>
        </form>
      </section>
    </div>
  )
}

export default AdminCatalogPage
