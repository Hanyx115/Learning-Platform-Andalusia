import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as enrollmentApi from '../../api/enrollment'
import CourseAssistantPanel from './CourseAssistantPanel'

describe('CourseAssistantPanel', () => {
  beforeEach(() => {
    vi.spyOn(enrollmentApi, 'askCourseAssistant').mockResolvedValue({
      answer: 'Start with the published networking workbook [Source 1].',
      citations: [
        {
          materialId: 'material-1',
          title: 'Networking workbook',
          resourceType: 'Link',
          resourceUrl: 'https://learning.example.com/networking.pdf',
          originalFileName: null,
          materialType: 'Document',
        },
      ],
      isDemoMode: true,
    })
  })

  it('sends a suggested question and displays a grounded answer with its source', async () => {
    const user = userEvent.setup()
    render(<CourseAssistantPanel enrollmentId="enrollment-1" />)

    await user.click(screen.getByRole('button', { name: 'Open Atlas' }))
    await user.click(screen.getByRole('button', { name: 'What key concepts should I review?' }))

    expect(await screen.findByText('Start with the published networking workbook [Source 1].')).toBeInTheDocument()
    expect(enrollmentApi.askCourseAssistant).toHaveBeenCalledWith(
      'enrollment-1',
      'What key concepts should I review?',
      'en',
      [],
    )
    expect(screen.getByRole('link', { name: 'Source 1: Networking workbook' })).toHaveAttribute(
      'href',
      'https://learning.example.com/networking.pdf',
    )
    expect(screen.getByText('Demo response')).toBeInTheDocument()
  })
})
