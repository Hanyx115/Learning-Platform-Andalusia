import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as communityApi from '../../api/community'
import CommunityPage from './CommunityPage'

describe('CommunityPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.spyOn(communityApi, 'getCommunity').mockResolvedValue({
      offeringTitle: 'Cybersecurity Foundations',
      courseOfferingId: 'offering-1',
      programOfferingId: null,
      canPost: true,
      canAnnounce: false,
      canModerate: false,
      page: 1,
      pageSize: 10,
      totalCount: 1,
      items: [{
        id: 'post-1',
        postType: 'Announcement',
        title: 'Next workshop',
        body: 'Bring your threat models.',
        authorUserId: 'instructor-1',
        authorName: 'Demo Instructor',
        isMine: false,
        isPinned: true,
        isLocked: false,
        createdAtUtc: '2026-11-09T10:00:00Z',
        updatedAtUtc: null,
        replies: [],
      }],
    })
  })

  it('shows the cohort feed while keeping instructor controls away from learners', async () => {
    render(
      <MemoryRouter initialEntries={['/community/course/offering-1']}>
        <Routes><Route path="/community/:offeringType/:offeringId" element={<CommunityPage />} /></Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Cybersecurity Foundations' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Next workshop' })).toBeInTheDocument()
    expect(screen.getByText('Pinned')).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Instructor announcement' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Lock replies' })).not.toBeInTheDocument()
  })
})
