import type { ReactNode } from 'react'
import Container from '../../components/Container'
import './auth.css'

interface AuthPageShellProps {
  eyebrow: string
  title: string
  description: string
  children: ReactNode
}

function AuthPageShell({ eyebrow, title, description, children }: AuthPageShellProps) {
  return (
    <div className="auth-page">
      <Container className="auth-page__container">
        <section className="auth-card">
          <p className="auth-card__eyebrow">{eyebrow}</p>
          <h1>{title}</h1>
          <p className="auth-card__lead">{description}</p>
          {children}
        </section>
      </Container>
    </div>
  )
}

export default AuthPageShell
