import type { ButtonHTMLAttributes } from 'react'

import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

type IconButtonSize = 'sm' | 'md' | 'lg'

const sizeClasses: Record<IconButtonSize, string> = {
  sm: 'size-7 text-[14px]',
  md: 'size-[34px] text-[16px]',
  lg: 'size-9 text-[16px]',
}

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  name: IconName
  /** Обязателен: у кнопки без подписи это единственное, что услышит скринридер. */
  label: string
  size?: IconButtonSize
  bordered?: boolean
}

/** Круглая кнопка с одной иконкой: закрыть модалку, листать страницы, открыть меню строки. */
export function IconButton({
  name,
  label,
  size = 'md',
  bordered = true,
  className,
  type = 'button',
  ...rest
}: IconButtonProps) {
  return (
    <button
      type={type}
      aria-label={label}
      title={label}
      className={cn(
        'text-ink-muted hover:bg-muted hover:text-ink inline-flex cursor-pointer items-center justify-center rounded-full transition-colors',
        'disabled:cursor-not-allowed disabled:opacity-45 disabled:hover:bg-transparent',
        bordered ? 'border-line bg-surface border' : 'border border-transparent',
        sizeClasses[size],
        className,
      )}
      {...rest}
    >
      <Icon name={name} />
    </button>
  )
}
