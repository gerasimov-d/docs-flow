import type { ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

type TagTone = 'sage' | 'accent' | 'neutral' | 'danger' | 'empty'

const toneClasses: Record<TagTone, string> = {
  sage: 'bg-sage-tint text-sage-deep',
  accent: 'bg-accent-tint text-accent-deep',
  neutral: 'bg-muted text-ink-dim',
  danger: 'bg-danger-tint text-danger-deep',
  // Пунктир — «значения нет», а не «значение такое». Отличается формой, а не только цветом.
  empty: 'border border-dashed border-line-dashed bg-muted text-ink-muted',
}

interface TagProps {
  tone?: TagTone
  icon?: IconName
  className?: string
  children: ReactNode
}

/** Маленькая плашка-метка: контекст документа, роль участника, признак способа поиска. */
export function Tag({ tone = 'neutral', icon, className, children }: TagProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-[11px] py-[3px] text-[12px] font-semibold whitespace-nowrap',
        toneClasses[tone],
        className,
      )}
    >
      {icon !== undefined && <Icon name={icon} className="text-[12px]" />}
      {children}
    </span>
  )
}
