import { apiRequest } from './client'

export type CommunityPostType = 'Discussion' | 'Announcement'

export interface CommunityReply {
  id: string
  authorUserId: string
  authorName: string
  isMine: boolean
  body: string
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CommunityPost {
  id: string
  postType: CommunityPostType
  title: string
  body: string
  authorUserId: string
  authorName: string
  isMine: boolean
  isPinned: boolean
  isLocked: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
  replies: CommunityReply[]
}

export interface CommunityPage {
  offeringTitle: string
  courseOfferingId: string | null
  programOfferingId: string | null
  canPost: boolean
  canAnnounce: boolean
  canModerate: boolean
  page: number
  pageSize: number
  totalCount: number
  items: CommunityPost[]
}

function scopePath(offeringType: string, offeringId: string) {
  return `/api/v1/community/${encodeURIComponent(offeringType.toLowerCase())}/${encodeURIComponent(offeringId)}`
}

export function getCommunity(offeringType: string, offeringId: string, page = 1, pageSize = 10) {
  return apiRequest<CommunityPage>(`${scopePath(offeringType, offeringId)}?page=${page}&pageSize=${pageSize}`)
}

export function createCommunityPost(
  offeringType: string,
  offeringId: string,
  request: { postType: CommunityPostType; title: string; body: string },
) {
  return apiRequest<CommunityPost>(`${scopePath(offeringType, offeringId)}/posts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
}

export function createCommunityReply(postId: string, body: string) {
  return apiRequest<CommunityReply>(`/api/v1/community/posts/${postId}/replies`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ body }),
  })
}

export function moderateCommunityPost(postId: string, isPinned: boolean, isLocked: boolean) {
  return apiRequest<CommunityPost>(`/api/v1/community/posts/${postId}/moderation`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isPinned, isLocked }),
  })
}
