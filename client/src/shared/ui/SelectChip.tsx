import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

export interface SelectChipOption {
  value: string
  label: string
}

interface SelectChipProps {
  label: string
  icon?: IconName
  value: string
  options: SelectChipOption[]
  onChange: (value: string) => void
  /** Значение «ничего не выбрано». Пока выбрано оно, чип выглядит спокойным. */
  neutralValue?: string
  className?: string
}

/**
 * Фильтр-таблетка с выпадающим списком.
 *
 * Внутри — родной `<select>`, прикрытый разметкой: он даёт клавиатуру, поиск по первой букве
 * и системный список на мобильных. Своё меню пришлось бы догонять по всем этим пунктам,
 * и обычно догоняют не полностью.
 */
export function SelectChip({
  label,
  icon,
  value,
  options,
  onChange,
  neutralValue = '',
  className,
}: SelectChipProps) {
  const active = value !== neutralValue
  const selected = options.find((option) => option.value === value)

  return (
    <span
      className={cn(
        'relative inline-flex items-center gap-1.5 rounded-full border py-[7px] pr-3 pl-3.5 text-[13px] transition-colors',
        active
          ? 'border-accent bg-accent-tint text-accent-deep font-bold'
          : 'border-line bg-surface text-ink-dim hover:border-line-dashed font-semibold',
        className,
      )}
    >
      {icon !== undefined && <Icon name={icon} className="text-[15px]" />}
      <span>
        {label}: {selected?.label ?? '—'}
      </span>
      <Icon name={active ? 'x' : 'chevron-down'} className="text-[14px]" />

      <select
        aria-label={label}
        value={value}
        onChange={(event) => {
          onChange(event.target.value)
        }}
        className="absolute inset-0 cursor-pointer opacity-0"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </span>
  )
}
