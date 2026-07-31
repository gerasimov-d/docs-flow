import type { ButtonHTMLAttributes } from 'react'

import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

interface FilterChipProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  icon?: IconName
  label: string
  /** Фильтр что-то сужает: чип окрашивается и вместо стрелки показывает крестик сброса. */
  active?: boolean
  onClear?: () => void
}

/**
 * Чип фильтра. Один и тот же компонент и в библиотеке, и под полем ввода в диалоге —
 * в дизайне это подчёркнуто: фильтры поиска и фильтры библиотеки совпадают, и выглядеть
 * они должны одинаково.
 */
export function FilterChip({
  icon,
  label,
  active = false,
  onClear,
  className,
  type = 'button',
  ...rest
}: FilterChipProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full border py-[5px] pr-2.5 pl-3 text-[12px] transition-colors',
        active
          ? 'border-accent bg-accent-tint text-accent-deep font-bold'
          : 'border-line bg-surface text-ink-dim hover:border-line-dashed font-semibold',
        className,
      )}
    >
      <button type={type} className="inline-flex cursor-pointer items-center gap-1.5" {...rest}>
        {icon !== undefined && <Icon name={icon} className="text-[13px]" />}
        {label}
      </button>
      {active && onClear !== undefined ? (
        <button
          type="button"
          aria-label={`Снять фильтр «${label}»`}
          onClick={onClear}
          className="cursor-pointer"
        >
          <Icon name="x" className="text-[12px]" />
        </button>
      ) : (
        <Icon name="chevron-down" className="text-[12px]" />
      )}
    </span>
  )
}
