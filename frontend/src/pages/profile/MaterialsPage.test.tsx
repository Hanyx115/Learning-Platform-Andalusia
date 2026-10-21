import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import type { CourseMaterial } from '../../api/enrollment'
import MaterialsPage from './MaterialsPage'

const material: CourseMaterial = {
  id: 'material-1',
  courseId: 'course-1',
  courseSessionId: 'session-1',
  sessionTitle: 'Network models and protocols',
  title: 'Networking workbook',
  description: 'Reference exercises for the course.',
  resourceUrl: 'https://learning.example.com/networking.pdf',
  materialType: 'Document',
  sortOrder: 0,
  visibility: 'Published',
  createdAtUtc: '2026-10-21T08:00:00Z',
  updatedAtUtc: null,
}

describe('MaterialsPage', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'getEnrollmentMaterials').mockResolvedValue([material])
  })

  it('shows published learning resources for the selected enrollment', async () => {
    render(
      <MemoryRouter initialEntries={['/profile/enrollments/enrollment-1/materials']}>
        <Routes>
          <Route path="/profile/enrollments/:enrollmentId/materials" element={<MaterialsPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Networking workbook' })).toBeInTheDocument()
    expect(screen.getByText('Session: Network models and protocols')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Open resource' })).toHaveAttribute(
      'href',
      'https://learning.example.com/networking.pdf',
    )
    expect(enrollmentApi.getEnrollmentMaterials).toHaveBeenCalledWith('enrollment-1')
  })
})
