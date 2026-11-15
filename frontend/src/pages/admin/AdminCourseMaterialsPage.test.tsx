import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AdminCourseMaterialsPage from './AdminCourseMaterialsPage'

const contentApi = vi.hoisted(() => ({
  createCourseMaterial: vi.fn(),
  createCourseMaterialFile: vi.fn(),
  downloadManagedCourseMaterialFile: vi.fn(),
  getCourseMaterialsForManagement: vi.fn(),
  updateCourseMaterial: vi.fn(),
  updateCourseMaterialMetadata: vi.fn(),
}))

vi.mock('../../api/content', () => contentApi)

describe('AdminCourseMaterialsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    contentApi.getCourseMaterialsForManagement.mockResolvedValue([])
    contentApi.createCourseMaterialFile.mockResolvedValue({})
  })

  it('lets an instructor upload a protected course file', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter initialEntries={['/instructor/courses/course-1/materials']}>
        <Routes>
          <Route path="/instructor/courses/:courseId/materials" element={<AdminCourseMaterialsPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByText('No materials have been added yet.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Upload file' }))
    await user.type(screen.getByLabelText('Title'), 'Week 1 handout')

    const file = new File(['course notes'], 'week-1.pdf', { type: 'application/pdf' })
    const fileInput = document.querySelector<HTMLInputElement>('input[type="file"]')
    expect(fileInput).not.toBeNull()
    fireEvent.change(fileInput!, { target: { files: [file] } })
    fireEvent.submit(screen.getByRole('button', { name: 'Create' }).closest('form')!)

    await waitFor(() => {
      expect(contentApi.createCourseMaterialFile).toHaveBeenCalledWith(
        'course-1',
        expect.objectContaining({
          title: 'Week 1 handout',
          materialType: 'Document',
          visibility: 'Published',
        }),
        file,
      )
    })
    expect(contentApi.createCourseMaterial).not.toHaveBeenCalled()
  })
})
