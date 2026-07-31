import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

export interface SegmentedOption<TValue extends string> {
  value: TValue
  label: string
  icon?: IconName
}

type SegmentedTone = 'accent' | 'surface'

interface SegmentedProps<TValue extends string> {
  options: SegmentedOption<TValue>[]
  value: TValue
  onChange: (value: TValue) => void
  /** Подпись группы для скринридера: «Вид библиотеки», «Режим запроса». */
  label: string
  /** `accent` — выбор режима, `surface` — выбор представления одного и того же. */
  tone?: SegmentedTone
  className?: string
}

/**
 * Переключатель из двух-трёх взаимоисключающих вариантов.
 *
 * Роль `radiogroup`, а не набор кнопок: варианты взаимоисключающие, и стрелками по ним
 * положено ходить, а не табом. Выбранный вариант отличается не только фоном — у него
 * `aria-checked`, иначе для скринридера все три пункта выглядят одинаково.
 */
export function Segmented<TValue extends string>({
  options,
  value,
  onChange,
  label,
  tone = 'accent',
  className,
}: SegmentedProps<TValue>) {
  const move = (delta: number) => {
    // `at` сам заворачивает отрицательный индекс, поэтому стрелка влево с первого варианта
    // попадает на последний без отдельной ветки.
    const next = options.at(
      (options.findIndex((option) => option.value === value) + delta) % options.length,
    )

    if (next !== undefined) {
      onChange(next.value)
    }
  }

  return (
    <div
      role="radiogroup"
      aria-label={label}
      className={cn(
        'border-line bg-raised inline-flex gap-[3px] rounded-full border p-[3px]',
        className,
      )}
      onKeyDown={(event) => {
        if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
          event.preventDefault()
          move(1)
        }

        if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
          event.preventDefault()
          move(-1)
        }
      }}
    >
      {options.map((option) => {
        const selected = option.value === value

        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={selected}
            tabIndex={selected ? 0 : -1}
            onClick={() => {
              onChange(option.value)
            }}
            className={cn(
              'inline-flex cursor-pointer items-center gap-1.5 rounded-full px-[13px] py-[5px] text-[12px] transition-colors',
              selected && tone === 'accent' && 'bg-accent text-on-accent font-bold',
              selected && tone === 'surface' && 'bg-surface text-ink shadow-card font-bold',
              !selected && 'text-ink-muted hover:text-ink font-semibold',
            )}
          >
            {option.icon !== undefined && <Icon name={option.icon} className="text-[13px]" />}
            {option.label}
          </button>
        )
      })}
    </div>
  )
}
