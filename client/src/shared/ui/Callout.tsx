import type { ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

type CalloutTone = 'info' | 'warning' | 'danger' | 'quiet'

const toneClasses: Record<CalloutTone, string> = {
  info: 'border border-line bg-surface text-ink-muted',
  warning: 'border border-accent-edge bg-accent-wash text-accent-deep',
  danger: 'border border-danger-edge bg-danger-wash text-danger-deep',
  // Без рамки — для пояснений, которые не предупреждают, а просто снимают вопрос.
  quiet: 'bg-raised text-ink-muted',
}

const toneIcons: Record<CalloutTone, IconName> = {
  info: 'info',
  warning: 'triangle-alert',
  danger: 'triangle-alert',
  quiet: 'info',
}

interface CalloutProps {
  tone?: CalloutTone
  icon?: IconName
  /** Действие справа: «Повторить обработку», «Снять фильтры». */
  action?: ReactNode
  className?: string
  children: ReactNode
}

/**
 * Пояснение к состоянию экрана: почему выдача такая, что сейчас происходит, чего система
 * не гарантирует. В дизайне такие блоки встречаются на каждом экране и всегда объясняют
 * причину, а не просто окрашивают предупреждение.
 */
export function Callout({ tone = 'info', icon, action, className, children }: CalloutProps) {
  return (
    <div
      className={cn(
        'flex items-center gap-3 rounded-2xl px-4 py-2.5 text-[12px] leading-[1.5]',
        toneClasses[tone],
        className,
      )}
    >
      <Icon name={icon ?? toneIcons[tone]} className="text-[16px]" />
      <span className="flex-1">{children}</span>
      {action}
    </div>
  )
}
