import { apiRequest } from './client'

export interface NotificationItem {
  id: string
  eventType: string
  titleEn: string
  titleAr: string
  messageEn: string
  messageAr: string
  linkUrl: string | null
  createdAtUtc: string
  readAtUtc: string | null
}

export interface NotificationList {
  items: NotificationItem[]
  unreadCount: number
}

export interface NotificationTemplate {
  id: string
  code: string
  titleEn: string
  titleAr: string
  messageEn: string
  messageAr: string
  isActive: boolean
  updatedAtUtc: string
}

export interface NotificationTemplateRequest {
  titleEn: string
  titleAr: string
  messageEn: string
  messageAr: string
  isActive: boolean
}

export interface AnnouncementRequest {
  recipientUserIds: string[]
  titleEn: string
  titleAr: string
  messageEn: string
  messageAr: string
  linkUrl?: string | null
}

const jsonHeaders = { 'Content-Type': 'application/json' }

export function getNotifications() {
  return apiRequest<NotificationList>('/api/v1/notifications')
}

export function markNotificationRead(id: string) {
  return apiRequest<void>(`/api/v1/notifications/${id}/read`, { method: 'PATCH' })
}

export function markAllNotificationsRead() {
  return apiRequest<void>('/api/v1/notifications/read-all', { method: 'PATCH' })
}

export function getNotificationTemplates() {
  return apiRequest<NotificationTemplate[]>('/api/v1/notifications/templates')
}

export function updateNotificationTemplate(id: string, request: NotificationTemplateRequest) {
  return apiRequest<NotificationTemplate>(`/api/v1/notifications/templates/${id}`, {
    method: 'PUT',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}

export function createAnnouncement(request: AnnouncementRequest) {
  return apiRequest<void>('/api/v1/notifications/announcements', {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request),
  })
}
