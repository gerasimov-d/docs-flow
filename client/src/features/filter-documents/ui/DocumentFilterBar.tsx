import { useContexts } from '@/entities/context'
import { SelectChip } from '@/shared/ui'

import { periodOptions } from '../model/use-document-filters'
import type { DocumentFilterState } from '../model/use-document-filters'

interface DocumentFilterBarProps {
  spaceId: string
  filters: DocumentFilterState
  onChange: <TKey extends keyof DocumentFilterState>(key: TKey, value: string) => void
  /** Статус не нужен под полем ввода в диалоге — там сужают выдачу, а не разбирают инбокс. */
  withStatus?: boolean
}

/**
 * Панель фильтров.
 *
 * Один и тот же компонент в библиотеке и под полем запроса — в дизайне это отдельно
 * оговорено: фильтры поиска совпадают с фильтрами библиотеки, и расходиться им нельзя.
 */
export function DocumentFilterBar({
  spaceId,
  filters,
  onChange,
  withStatus = true,
}: DocumentFilterBarProps) {
  const { contexts } = useContexts(spaceId)

  return (
    <>
      <SelectChip
        label="Контекст"
        icon="tag"
        value={filters.contextId}
        onChange={(value) => {
          onChange('contextId', value)
        }}
        options={[
          { value: '', label: 'все' },
          ...contexts.map((context) => ({ value: context.id, label: context.name })),
          { value: 'none', label: 'без контекста' },
        ]}
      />

      <SelectChip
        label="Период"
        icon="calendar"
        value={filters.period}
        onChange={(value) => {
          onChange('period', value)
        }}
        options={periodOptions}
      />

      <SelectChip
        label="Тип файла"
        icon="file-text"
        value={filters.kind}
        onChange={(value) => {
          onChange('kind', value)
        }}
        options={[
          { value: '', label: 'все' },
          { value: 'pdf', label: 'PDF' },
          { value: 'image', label: 'сканы и фото' },
        ]}
      />

      {withStatus && (
        <SelectChip
          label="Статус"
          icon="sliders-horizontal"
          value={filters.status}
          onChange={(value) => {
            onChange('status', value)
          }}
          options={[
            { value: '', label: 'любой' },
            { value: 'ready', label: 'готов' },
            { value: 'processing', label: 'распознаётся' },
            { value: 'accepted', label: 'принят' },
            { value: 'failed', label: 'ошибка' },
          ]}
        />
      )}
    </>
  )
}
