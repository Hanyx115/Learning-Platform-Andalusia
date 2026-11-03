import { type FormEvent, type KeyboardEvent, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  askCourseAssistant,
  type CourseAssistantCitation,
  type CourseAssistantHistoryMessage,
} from '../../api/enrollment'
import { getApiErrorMessage } from '../../api/auth'

interface CourseAssistantPanelProps {
  enrollmentId: string
}

interface ChatMessage extends CourseAssistantHistoryMessage {
  id: string
  citations?: CourseAssistantCitation[]
  isDemoMode?: boolean
}

function CourseAssistantPanel({ enrollmentId }: CourseAssistantPanelProps) {
  const { i18n, t } = useTranslation()
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [draft, setDraft] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [error, setError] = useState('')

  const quickPrompts = [
    t('courseAssistant.prompts.summarize'),
    t('courseAssistant.prompts.concepts'),
    t('courseAssistant.prompts.practice'),
  ]

  async function sendQuestion(question: string) {
    const normalizedQuestion = question.trim()
    if (!normalizedQuestion || isSending) {
      return
    }

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: normalizedQuestion,
    }
    const history = messages.slice(-6).map(({ role, content }) => ({ role, content }))

    setMessages((previous) => [...previous, userMessage])
    setDraft('')
    setError('')
    setIsSending(true)

    try {
      const response = await askCourseAssistant(
        enrollmentId,
        normalizedQuestion,
        i18n.language,
        history,
      )

      setMessages((previous) => [
        ...previous,
        {
          id: crypto.randomUUID(),
          role: 'assistant',
          content: response.answer,
          citations: response.citations,
          isDemoMode: response.isDemoMode,
        },
      ])
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, t('courseAssistant.errors.send')))
    } finally {
      setIsSending(false)
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    void sendQuestion(draft)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void sendQuestion(draft)
    }
  }

  return (
    <section className="course-assistant" aria-labelledby="course-assistant-title">
      <div className="course-assistant__header">
        <div className="course-assistant__identity">
          <span aria-hidden="true">OS</span>
          <div>
            <p>{t('courseAssistant.eyebrow')}</p>
            <h2 id="course-assistant-title">{t('courseAssistant.name')}</h2>
          </div>
        </div>
        {messages.length > 0 ? (
          <button className="course-assistant__reset" type="button" onClick={() => {
            setMessages([])
            setError('')
          }}>
            {t('courseAssistant.newConversation')}
          </button>
        ) : null}
      </div>

      <p className="course-assistant__description">{t('courseAssistant.description')}</p>

      {messages.length === 0 ? (
        <div className="course-assistant__welcome">
          <strong>{t('courseAssistant.welcome')}</strong>
          <div className="course-assistant__prompts">
            {quickPrompts.map((prompt) => (
              <button key={prompt} type="button" onClick={() => void sendQuestion(prompt)}>
                {prompt}
              </button>
            ))}
          </div>
        </div>
      ) : (
        <div className="course-assistant__messages" role="log" aria-live="polite" aria-relevant="additions">
          {messages.map((message) => (
            <article
              key={message.id}
              className={`course-assistant__message course-assistant__message--${message.role}`}
            >
              <strong>{message.role === 'assistant' ? t('courseAssistant.name') : t('courseAssistant.you')}</strong>
              <p dir="auto">{message.content}</p>
              {message.isDemoMode ? (
                <span className="course-assistant__demo">{t('courseAssistant.demoMode')}</span>
              ) : null}
              {message.citations && message.citations.length > 0 ? (
                <div className="course-assistant__sources">
                  <strong>{t('courseAssistant.sources')}</strong>
                  <ul>
                    {message.citations.map((citation, index) => (
                      <li key={citation.materialId}>
                        <a href={citation.resourceUrl} target="_blank" rel="noreferrer">
                          {t('courseAssistant.sourceLabel', { number: index + 1, title: citation.title })}
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </article>
          ))}
          {isSending ? (
            <p className="course-assistant__thinking" role="status">{t('courseAssistant.thinking')}</p>
          ) : null}
        </div>
      )}

      {error ? <p className="auth-message auth-message--error" role="alert">{error}</p> : null}

      <form className="course-assistant__composer" onSubmit={handleSubmit}>
        <label htmlFor="course-assistant-question">{t('courseAssistant.questionLabel')}</label>
        <div>
          <textarea
            id="course-assistant-question"
            value={draft}
            maxLength={1000}
            rows={2}
            placeholder={t('courseAssistant.placeholder')}
            disabled={isSending}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={handleKeyDown}
          />
          <button className="button button--primary" type="submit" disabled={isSending || !draft.trim()}>
            {isSending ? t('courseAssistant.sending') : t('courseAssistant.send')}
          </button>
        </div>
        <small>{t('courseAssistant.disclaimer')}</small>
      </form>
    </section>
  )
}

export default CourseAssistantPanel
