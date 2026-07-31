import type { ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

interface EmptyStateProps {
  icon: IconName
  title: string
  description: ReactNode
  tone?: 'accent' | 'neutral'
  actions?: ReactNode
  className?: string
}

/**
 * Пустое состояние: почему пусто и что с этим делать.
 *
 * Описание и действия — обязательная часть, а не украшение: экран «ничего нет» без причины
 * и без выхода читается как поломка.
 */
export function EmptyState({
  icon,
  title,
  description,
  tone = 'accent',
  actions,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn('flex flex-col items-center justify-center gap-3.5 p-7 text-center', className)}
    >
      <span
        className={cn(
          'inline-flex size-12 items-center justify-center rounded-full',
          tone === 'accent' ? 'bg-accent-tint text-accent' : 'bg-muted text-ink-muted',
        )}
      >
        <Icon name={icon} className="text-[22px]" />
      </span>
      <h2 className="font-display text-[20px] leading-[1.15] font-extrabold tracking-[-0.015em]">
        {title}
      </h2>
      <p className="text-ink-muted max-w-[440px] text-[13px] leading-[1.6] text-pretty">
        {description}
      </p>
      {actions !== undefined && (
        <div className="flex flex-wrap justify-center gap-2.5">{actions}</div>
      )}
    </div>
  )
}
