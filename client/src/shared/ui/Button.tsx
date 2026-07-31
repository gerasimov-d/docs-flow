import type { ButtonHTMLAttributes, ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { Icon } from './Icon'
import type { IconName } from './glyphs'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'dangerOutline'
type ButtonSize = 'sm' | 'md' | 'lg'

/*
 * Варианты не пересекаются по свойствам: каждый задаёт свои фон, рамку и цвет текста
 * целиком. Поэтому классы можно склеивать простым `cn`, не разрешая конфликты Tailwind.
 */
const variantClasses: Record<ButtonVariant, string> = {
  primary:
    'bg-accent border-accent text-on-accent hover:bg-accent-strong hover:border-accent-strong',
  secondary: 'bg-transparent border-line text-ink hover:bg-muted',
  ghost: 'bg-transparent border-transparent text-ink-dim hover:bg-muted',
  danger: 'bg-danger border-danger text-surface hover:bg-danger-deep hover:border-danger-deep',
  dangerOutline:
    'bg-transparent border-danger-edge text-danger-deep hover:bg-danger-tint hover:border-danger',
}

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'gap-[7px] px-[15px] py-[7px] text-[13px]',
  md: 'gap-2 px-[18px] py-[9px] text-[14px]',
  lg: 'gap-[9px] px-[26px] py-3 text-[16px]',
}

const iconSizeClasses: Record<ButtonSize, string> = {
  sm: 'text-[14px]',
  md: 'text-[16px]',
  lg: 'text-[18px]',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  /** Иконка перед подписью. Кнопка без подписи — это `IconButton`, а не пустой `children`. */
  icon?: IconName
  children: ReactNode
}

/**
 * Кнопка действия. Форма — таблетка, шрифт — display: в этой дизайн-системе кнопка звучит
 * так же громко, как заголовок, и это её опознавательный знак.
 */
export function Button({
  variant = 'secondary',
  size = 'md',
  icon,
  className,
  type = 'button',
  children,
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cn(
        'font-display inline-flex cursor-pointer items-center justify-center rounded-full border font-bold whitespace-nowrap transition-colors',
        'disabled:cursor-not-allowed disabled:opacity-45 disabled:hover:bg-transparent',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      {...rest}
    >
      {icon !== undefined && <Icon name={icon} className={iconSizeClasses[size]} />}
      {children}
    </button>
  )
}
