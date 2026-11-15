import type { ElementType, ReactNode } from 'react'
import './Card.css'

interface CardProps {
  as?: ElementType
  className?: string
  children: ReactNode
}

function Card({ as: Component = 'div', className, children }: CardProps) {
  const classes = className ? `card ${className}` : 'card'

  return <Component className={classes}>{children}</Component>
}

export default Card
