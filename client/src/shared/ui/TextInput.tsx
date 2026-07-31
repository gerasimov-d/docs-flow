import type { InputHTMLAttributes } from 'react'

import { cn } from '@/shared/lib'

type TextInputProps = InputHTMLAttributes<HTMLInputElement>

/**
 * Поле ввода в форме таблетки — та же геометрия, что у кнопок рядом.
 *
 * Своей рамки фокуса не рисует: глобальный `:focus-visible` в теме уже даёт акцентное
 * кольцо, и второй индикатор поверх него читался бы как ошибка.
 */
export function TextInput({ className, ...rest }: TextInputProps) {
  return (
    <input
      className={cn(
        'border-line bg-page text-ink w-full rounded-full border px-4 py-2.5 text-[14px] font-semibold',
        'placeholder:text-ink-faint placeholder:font-normal',
        'disabled:cursor-not-allowed disabled:opacity-45',
        className,
      )}
      {...rest}
    />
  )
}
