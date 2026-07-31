import { cn } from '@/shared/lib'

import { documentStatusPresentation } from '../model/document'
import type { DocumentStatus } from '../model/document'

interface DocumentStatusBadgeProps {
  status: DocumentStatus
  className?: string
}

/**
 * Статус документа плашкой с точкой.
 *
 * Точка дублирует цвет формой присутствия, но текст остаётся главным: различать четыре
 * состояния по одному оттенку — значит потерять их для всех, кто цвет различает хуже.
 */
export function DocumentStatusBadge({ status, className }: DocumentStatusBadgeProps) {
  const { label, pill, dot } = documentStatusPresentation[status]

  return (
    <span
      className={cn(
        'inline-flex items-center gap-[7px] rounded-full px-3 py-1 text-[12px] font-bold whitespace-nowrap',
        pill,
        className,
      )}
    >
      <span className={cn('size-[7px] rounded-full', dot)} />
      {label}
    </span>
  )
}
