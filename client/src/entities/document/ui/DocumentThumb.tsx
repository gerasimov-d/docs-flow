import { cn } from '@/shared/lib'
import { Icon } from '@/shared/ui'

import type { DocumentKind } from '../model/document'

interface DocumentThumbProps {
  kind: DocumentKind
  /** Сбойный документ отличается и рамкой: в длинном списке одного цвета иконки мало. */
  failed?: boolean
  className?: string
}

/**
 * Миниатюра документа.
 *
 * Показывает тип файла иконкой, а не превью страницы: превью пришлось бы тянуть отдельным
 * запросом на каждую строку списка, и до готовности распознавания его всё равно нет.
 */
export function DocumentThumb({ kind, failed = false, className }: DocumentThumbProps) {
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center justify-center rounded-lg border',
        failed
          ? 'border-danger-edge bg-danger-tint text-danger-deep'
          : 'border-line bg-muted text-ink-subtle',
        'h-[52px] w-10',
        className,
      )}
    >
      <Icon name={kind === 'pdf' ? 'file-text' : 'image'} className="text-[18px]" />
    </span>
  )
}
