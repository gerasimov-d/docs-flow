import type { ReactNode } from 'react'

import { cn } from '@/shared/lib'

interface SectionLabelProps {
  className?: string
  children: ReactNode
}

/** Тихий разделитель-заголовок: «Архив», «Источники», «Эта сессия», «Оригинал». */
export function SectionLabel({ className, children }: SectionLabelProps) {
  return (
    <div
      className={cn('text-ink-subtle text-[11px] font-bold tracking-[0.08em] uppercase', className)}
    >
      {children}
    </div>
  )
}
