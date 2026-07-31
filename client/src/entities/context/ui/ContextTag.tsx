import { Tag } from '@/shared/ui'

interface ContextTagProps {
  /** `null` — у документа контекста нет. Это не ошибка: контекст необязателен. */
  name: string | null
  withIcon?: boolean
  className?: string
}

/**
 * Метка контекста документа.
 *
 * «Медицина» подсвечена шалфеем, «Авто» — акцентом, остальные нейтральны: в дизайне цвет
 * закреплён за контекстом, а не выдаётся по порядку. Отсутствие контекста показано пунктиром —
 * формой, а не только цветом, чтобы «нет значения» не читалось как ещё одно значение.
 */
export function ContextTag({ name, withIcon = false, className }: ContextTagProps) {
  if (name === null) {
    return (
      <Tag tone="empty" className={className}>
        Без контекста
      </Tag>
    )
  }

  return (
    <Tag tone={toneOf(name)} icon={withIcon ? 'tag' : undefined} className={className}>
      {name}
    </Tag>
  )
}

function toneOf(name: string): 'sage' | 'accent' | 'neutral' {
  if (name === 'Медицина') {
    return 'sage'
  }

  return name === 'Авто' ? 'accent' : 'neutral'
}
