import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { getApiErrorMessage, getApiErrorStatus } from '../../api/auth'
import {
  createCommunityPost,
  createCommunityReply,
  getCommunity,
  moderateCommunityPost,
  type CommunityPage as CommunityPageData,
  type CommunityPostType,
} from '../../api/community'
import Container from '../../components/Container'
import './community.css'

function CommunityPage() {
  const { t, i18n } = useTranslation()
  const { offeringType = '', offeringId = '' } = useParams()
  const [community, setCommunity] = useState<CommunityPageData | null>(null)
  const [page, setPage] = useState(1)
  const [error, setError] = useState('')
  const [isForbidden, setIsForbidden] = useState(false)
  const [postType, setPostType] = useState<CommunityPostType>('Discussion')
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [replyDrafts, setReplyDrafts] = useState<Record<string, string>>({})
  const [workingKey, setWorkingKey] = useState('')

  const load = useCallback(() => {
    if (!offeringId || !['course', 'program'].includes(offeringType.toLowerCase())) return
    getCommunity(offeringType, offeringId, page)
      .then(setCommunity)
      .catch((requestError: unknown) => {
        if ([403, 404].includes(getApiErrorStatus(requestError) ?? 0)) setIsForbidden(true)
        else setError(getApiErrorMessage(requestError, t('community.errors.load')))
      })
  }, [offeringId, offeringType, page, t])

  useEffect(() => { load() }, [load])

  async function handleCreatePost(event: FormEvent) {
    event.preventDefault()
    setWorkingKey('post')
    setError('')
    try {
      await createCommunityPost(offeringType, offeringId, { postType, title, body })
      setTitle('')
      setBody('')
      setPage(1)
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('community.errors.post')))
    } finally {
      setWorkingKey('')
    }
  }

  async function handleReply(postId: string) {
    const reply = (replyDrafts[postId] ?? '').trim()
    if (!reply) return
    setWorkingKey(`reply-${postId}`)
    setError('')
    try {
      await createCommunityReply(postId, reply)
      setReplyDrafts((current) => ({ ...current, [postId]: '' }))
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('community.errors.reply')))
    } finally {
      setWorkingKey('')
    }
  }

  async function handleModeration(postId: string, isPinned: boolean, isLocked: boolean) {
    setWorkingKey(`moderate-${postId}`)
    setError('')
    try {
      await moderateCommunityPost(postId, isPinned, isLocked)
      load()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('community.errors.moderate')))
    } finally {
      setWorkingKey('')
    }
  }

  if (isForbidden) {
    return <Container className="auth-state-page"><h1>{t('community.unavailableTitle')}</h1><p>{t('community.unavailable')}</p></Container>
  }

  const totalPages = community ? Math.max(1, Math.ceil(community.totalCount / community.pageSize)) : 1

  return (
    <Container className="community-page">
      <div className="dashboard-section__heading community-page__heading">
        <span className="page-eyebrow">{t('community.eyebrow')}</span>
        <h1>{community?.offeringTitle ?? t('community.title')}</h1>
        <p>{t('community.description')}</p>
      </div>
      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}
      {!community && !error ? <p role="status">{t('community.loading')}</p> : null}

      {community?.canPost ? (
        <form className="community-composer" onSubmit={handleCreatePost}>
          <div className="community-composer__heading">
            <div><h2>{t('community.startConversation')}</h2><p>{t('community.composerHint')}</p></div>
            {community.canAnnounce ? (
              <label>{t('community.postType')}
                <select value={postType} onChange={(event) => setPostType(event.target.value as CommunityPostType)}>
                  <option value="Discussion">{t('community.types.Discussion')}</option>
                  <option value="Announcement">{t('community.types.Announcement')}</option>
                </select>
              </label>
            ) : null}
          </div>
          <label>{t('community.subject')}<input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} required /></label>
          <label>{t('community.message')}<textarea value={body} onChange={(event) => setBody(event.target.value)} maxLength={5000} required /></label>
          <button className="button button--primary" disabled={workingKey === 'post'}>{workingKey === 'post' ? t('community.posting') : t('community.publish')}</button>
        </form>
      ) : null}

      {community?.items.length === 0 ? <div className="community-empty"><h2>{t('community.emptyTitle')}</h2><p>{t('community.empty')}</p></div> : null}
      <div className="community-feed">
        {community?.items.map((post) => (
          <article className={`community-post community-post--${post.postType.toLowerCase()}`} key={post.id}>
            <header>
              <div>
                <div className="community-post__badges">
                  <span>{t(`community.types.${post.postType}`)}</span>
                  {post.isPinned ? <span>{t('community.pinned')}</span> : null}
                  {post.isLocked ? <span>{t('community.locked')}</span> : null}
                </div>
                <h2 dir="auto">{post.title}</h2>
                <p>{t('community.byline', { name: post.authorName, date: new Date(post.createdAtUtc).toLocaleString(i18n.language) })}</p>
              </div>
              {community.canModerate ? (
                <div className="community-post__moderation">
                  <button type="button" onClick={() => handleModeration(post.id, !post.isPinned, post.isLocked)} disabled={workingKey === `moderate-${post.id}`}>{post.isPinned ? t('community.unpin') : t('community.pin')}</button>
                  <button type="button" onClick={() => handleModeration(post.id, post.isPinned, !post.isLocked)} disabled={workingKey === `moderate-${post.id}`}>{post.isLocked ? t('community.unlock') : t('community.lock')}</button>
                </div>
              ) : null}
            </header>
            <p className="community-post__body" dir="auto">{post.body}</p>
            <section className="community-replies" aria-label={t('community.replies')}>
              <h3>{t('community.replyCount', { count: post.replies.length })}</h3>
              {post.replies.map((reply) => (
                <div className="community-reply" key={reply.id}>
                  <div><strong dir="auto">{reply.authorName}</strong><time dateTime={reply.createdAtUtc}>{new Date(reply.createdAtUtc).toLocaleString(i18n.language)}</time></div>
                  <p dir="auto">{reply.body}</p>
                </div>
              ))}
              {!post.isLocked && community.canPost ? (
                <div className="community-reply-form">
                  <label className="sr-only" htmlFor={`reply-${post.id}`}>{t('community.reply')}</label>
                  <textarea id={`reply-${post.id}`} value={replyDrafts[post.id] ?? ''} onChange={(event) => setReplyDrafts((current) => ({ ...current, [post.id]: event.target.value }))} placeholder={t('community.replyPlaceholder')} maxLength={3000} />
                  <button className="button button--secondary" type="button" disabled={!replyDrafts[post.id]?.trim() || workingKey === `reply-${post.id}`} onClick={() => handleReply(post.id)}>{t('community.reply')}</button>
                </div>
              ) : null}
            </section>
          </article>
        ))}
      </div>

      {community && totalPages > 1 ? (
        <nav className="catalog-pagination" aria-label={t('community.pagination')}>
          <button type="button" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>{t('common.previous')}</button>
          <span>{t('common.pageSummary', { page, totalPages })}</span>
          <button type="button" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>{t('common.next')}</button>
        </nav>
      ) : null}
    </Container>
  )
}

export default CommunityPage
