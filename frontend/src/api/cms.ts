import { apiRequest } from './client'

export type CmsContentType =
  | 'HomepageSection'
  | 'PublicPage'
  | 'Faq'
  | 'Partner'
  | 'Testimonial'
  | 'InstructorProfile'

export interface CmsContentItem {
  id: string
  contentType: CmsContentType
  key: string
  titleEn: string
  titleAr: string
  bodyEn: string
  bodyAr: string
  imageUrl: string | null
  linkUrl: string | null
  relatedUserId: string | null
  displayOrder: number
  isPublished: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CmsContentRequest {
  contentType: CmsContentType
  key: string
  titleEn: string
  titleAr: string
  bodyEn: string
  bodyAr: string
  imageUrl?: string | null
  linkUrl?: string | null
  relatedUserId?: string | null
  displayOrder: number
  isPublished: boolean
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function getPublishedContent(type?: CmsContentType) {
  const query = type ? `?type=${encodeURIComponent(type)}` : ''
  return apiRequest<CmsContentItem[]>(`/api/v1/content${query}`, {}, { includeAccessToken: false })
}

export function getPublicPage(key: string) {
  return apiRequest<CmsContentItem>(`/api/v1/content/pages/${encodeURIComponent(key)}`, {}, { includeAccessToken: false })
}

export function getFaqs() {
  return apiRequest<CmsContentItem[]>('/api/v1/faqs', {}, { includeAccessToken: false })
}

export function getCmsContent(type?: CmsContentType) {
  const query = type ? `?type=${encodeURIComponent(type)}` : ''
  return apiRequest<CmsContentItem[]>(`/api/v1/cms/content${query}`)
}

export function createCmsContent(request: CmsContentRequest) {
  return apiRequest<CmsContentItem>('/api/v1/cms/content', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function updateCmsContent(id: string, request: CmsContentRequest) {
  return apiRequest<CmsContentItem>(`/api/v1/cms/content/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function setCmsPublication(id: string, isPublished: boolean) {
  return apiRequest<CmsContentItem>(`/api/v1/cms/content/${id}/publication`, {
    method: 'PATCH',
    headers: jsonHeaders,
    body: JSON.stringify({ isPublished }),
  })
}

export function deleteCmsContent(id: string) {
  return apiRequest<void>(`/api/v1/cms/content/${id}`, { method: 'DELETE' })
}
